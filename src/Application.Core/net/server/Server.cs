/*
 This file is part of the OdinMS Maple Story Server
 Copyright (C) 2008 Patrick Huy <patrick.huy@frz.cc>
 Matthias Butz <matze@odinms.de>
 Jan Christian Meyer <vimes@odinms.de>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Affero General Public License as
 published by the Free Software Foundation version 3 as published by
 the Free Software Foundation. You may not use, modify or distribute
 this program under any other version of the GNU Affero General Public
 License.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Affero General Public License for more details.

 You should have received a copy of the GNU Affero General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */


using Application.Core.model;
using client;
using client.command;
using client.inventory;
using client.inventory.manipulator;
using client.newyear;
using client.processor.npc;
using constants.game;
using constants.inventory;
using constants.net;
using database.note;
using Microsoft.EntityFrameworkCore;
using net.netty;
using net.packet;
using net.server.channel;
using net.server.coordinator.session;
using net.server.guild;
using net.server.task;
using server;
using server.expeditions;
using server.life;
using server.quest;
using service;
using System.Diagnostics;
using static server.CashShop;

namespace net.server;

public class Server
{
    private static ILogger log = LogFactory.GetLogger(LogType.Server);
    private static Lazy<Server> instance = new Lazy<Server>(new Server());

    public static Server getInstance() => instance.Value;

    private static HashSet<int> activeFly = new();
    private static Dictionary<int, int> couponRates = new(30);
    private static List<int> activeCoupons = new();
    private static ChannelDependencies channelDependencies;

    private LoginServer loginServer;
    private List<Dictionary<int, string>> channels = new();
    private List<World> worlds = new();
    private Dictionary<string, string> subnetInfo = new();
    /// <summary>
    /// �����˺�id-��ɫid
    /// </summary>
    private Dictionary<int, HashSet<int>> accountChars = new();
    private Dictionary<int, short> accountCharacterCount = new();
    private Dictionary<int, int> worldChars = new();
    private Dictionary<string, int> transitioningChars = new();
    private List<KeyValuePair<int, string>> _worldRecommendedList = new();
    private Dictionary<int, Guild> guilds = new(100);
    private Dictionary<Client, long> inLoginState = new(100);

    private PlayerBuffStorage buffStorage = new PlayerBuffStorage();
    private Dictionary<int, Alliance> alliances = new(100);
    private Dictionary<int, NewYearCardRecord> newyears = new();
    private List<Client> processDiseaseAnnouncePlayers = new();
    private List<Client> registeredDiseaseAnnouncePlayers = new();

    private List<List<KeyValuePair<string, int>>> playerRanking = new();

    private object srvLock = new object();
    private object disLock = new object();

    private AtomicLong currentTime = new AtomicLong(0);
    private long serverCurrentTime = 0;

    private volatile bool availableDeveloperRoom = false;
    private bool online = false;
    public static DateTimeOffset uptime = DateTimeOffset.Now;
    ReaderWriterLockSlim wldLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    ReaderWriterLockSlim lgnLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    private Server()
    {
    }

    public int getCurrentTimestamp()
    {
        return (int)(Server.getInstance().getCurrentTime() - Server.uptime.ToUnixTimeMilliseconds());
    }

    public long getCurrentTime()
    {  // returns a slightly delayed time value, under frequency of UPDATE_INTERVAL
        return serverCurrentTime;
    }

    public void updateCurrentTime()
    {
        serverCurrentTime = currentTime.addAndGet(YamlConfig.config.server.UPDATE_INTERVAL);
    }

    public long forceUpdateCurrentTime()
    {
        long timeNow = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        serverCurrentTime = timeNow;
        currentTime.set(timeNow);

        return timeNow;
    }

    public bool isOnline()
    {
        return online;
    }

    public List<KeyValuePair<int, string>> worldRecommendedList()
    {
        return _worldRecommendedList;
    }

    public void setNewYearCard(NewYearCardRecord nyc)
    {
        newyears.AddOrUpdate(nyc.getId(), nyc);
    }

    public NewYearCardRecord? getNewYearCard(int cardid)
    {
        return newyears.GetValueOrDefault(cardid);
    }

    public NewYearCardRecord? removeNewYearCard(int cardid)
    {
        if (newyears.Remove(cardid, out var d))
            return d;
        return null;
    }

    public void setAvailableDeveloperRoom()
    {
        availableDeveloperRoom = true;
    }

    public bool canEnterDeveloperRoom()
    {
        return availableDeveloperRoom;
    }

    private void loadPlayerNpcMapStepFromDb()
    {
        List<World> wlist = this.getWorlds();

        using var dbContext = new DBContext();
        var list = dbContext.PlayernpcsFields.AsNoTracking().ToList();
        list.ForEach(rs =>
        {
            World? w = wlist.FirstOrDefault(x => x.getId() == rs.World);
            if (w != null)
                w.setPlayerNpcMapData(rs.Map, rs.Step, rs.Podium);
        });
    }

    public World? getWorld(int id)
    {
        wldLock.EnterReadLock();
        try
        {
            return worlds.ElementAtOrDefault(id);
        }
        finally
        {
            wldLock.ExitReadLock();
        }
    }

    public List<World> getWorlds()
    {
        wldLock.EnterReadLock();
        try
        {
            return worlds.ToList();
        }
        finally
        {
            wldLock.ExitReadLock();
        }
    }

    public int getWorldsSize()
    {
        wldLock.EnterReadLock();
        try
        {
            return worlds.Count;
        }
        finally
        {
            wldLock.ExitReadLock();
        }
    }

    public Channel? getChannel(int world, int channel)
    {
        try
        {
            return this.getWorld(world)?.getChannel(channel);
        }
        catch (NullReferenceException npe)
        {
            return null;
        }
    }

    public List<Channel> getChannelsFromWorld(int world)
    {
        try
        {
            return this.getWorld(world)?.getChannels() ?? [];
        }
        catch (NullReferenceException npe)
        {
            return new(0);
        }
    }

    public List<Channel> getAllChannels()
    {
        try
        {
            List<Channel> channelz = new();
            foreach (World world in this.getWorlds())
            {
                channelz.AddRange(world.getChannels());
            }
            return channelz;
        }
        catch (NullReferenceException npe)
        {
            return new(0);
        }
    }

    public HashSet<int> getOpenChannels(int world)
    {
        wldLock.EnterReadLock();
        try
        {
            return new(channels.get(world).Keys);
        }
        finally
        {
            wldLock.ExitReadLock();
        }
    }

    private string? getIP(int world, int channel)
    {
        wldLock.EnterReadLock();
        try
        {
            return channels.get(world).GetValueOrDefault(channel);
        }
        finally
        {
            wldLock.ExitReadLock();
        }
    }

    public string[] getInetSocket(Client client, int world, int channel)
    {
        string remoteIp = client.getRemoteAddress();

        string[] hostAddress = getIP(world, channel).Split(":");
        if (IpAddresses.isLocalAddress(remoteIp))
        {
            hostAddress[0] = YamlConfig.config.server.LOCALHOST;
        }
        else if (IpAddresses.isLanAddress(remoteIp))
        {
            hostAddress[0] = YamlConfig.config.server.LANHOST;
        }

        try
        {
            return hostAddress;
        }
        catch (Exception e)
        {
            return null;
        }
    }


    private void dumpData()
    {
        wldLock.EnterReadLock();
        try
        {
            log.Debug("Worlds: {Worlds}", worlds);
            log.Debug("Channels: {Channels}", channels);
            log.Debug("World recommended list: {RecommendedWorlds}", _worldRecommendedList);
            log.Debug("---------------------");
        }
        finally
        {
            wldLock.ExitReadLock();
        }
    }

    public int addChannel(int worldid)
    {
        World? world;
        Dictionary<int, string> channelInfo;
        int channelid;

        wldLock.EnterReadLock();
        try
        {
            if (worldid >= worlds.Count)
            {
                return -3;
            }

            channelInfo = channels.get(worldid);
            if (channelInfo == null)
            {
                return -3;
            }

            channelid = channelInfo.Count;
            if (channelid >= YamlConfig.config.server.CHANNEL_SIZE)
            {
                return -2;
            }

            channelid++;
            world = this.getWorld(worldid);
        }
        finally
        {
            wldLock.ExitReadLock();
        }

        Channel channel = new Channel(worldid, channelid, getCurrentTime());
        channel.setServerMessage(YamlConfig.config.worlds.get(worldid).why_am_i_recommended);

        if (world.addChannel(channel))
        {
            wldLock.EnterWriteLock();
            try
            {
                channelInfo.AddOrUpdate(channelid, channel.getIP());
            }
            finally
            {
                wldLock.ExitWriteLock();
            }
        }

        return channelid;
    }

    public int addWorld()
    {
        int newWorld = initWorld();
        if (newWorld > -1)
        {
            installWorldPlayerRanking(newWorld);

            HashSet<int> accounts;
            lgnLock.EnterReadLock();
            try
            {
                accounts = new(accountChars.Keys);
            }
            finally
            {
                lgnLock.ExitReadLock();
            }

            foreach (int accId in accounts)
            {
                loadAccountCharactersView(accId, 0, newWorld);
            }
        }

        return newWorld;
    }

    private int initWorld()
    {
        int i;

        wldLock.EnterReadLock();
        try
        {
            i = worlds.Count;

            if (i >= YamlConfig.config.server.WLDLIST_SIZE)
            {
                return -1;
            }
        }
        finally
        {
            wldLock.ExitReadLock();
        }

        log.Information("Starting world {WorldId}", i);

        int exprate = YamlConfig.config.worlds.get(i).exp_rate;
        int mesorate = YamlConfig.config.worlds.get(i).meso_rate;
        int droprate = YamlConfig.config.worlds.get(i).drop_rate;
        int bossdroprate = YamlConfig.config.worlds.get(i).boss_drop_rate;
        int questrate = YamlConfig.config.worlds.get(i).quest_rate;
        int travelrate = YamlConfig.config.worlds.get(i).travel_rate;
        int fishingrate = YamlConfig.config.worlds.get(i).fishing_rate;

        int flag = YamlConfig.config.worlds.get(i).flag;
        string event_message = YamlConfig.config.worlds.get(i).event_message;
        string why_am_i_recommended = YamlConfig.config.worlds.get(i).why_am_i_recommended;

        World world = new World(i,
                flag,
                event_message,
                exprate, droprate, bossdroprate, mesorate, questrate, travelrate, fishingrate);

        Dictionary<int, string> channelInfo = new();
        long bootTime = getCurrentTime();
        for (int j = 1; j <= YamlConfig.config.worlds.get(i).channels; j++)
        {
            int channelid = j;
            Channel channel = new Channel(i, channelid, bootTime);

            world.addChannel(channel);
            channelInfo.AddOrUpdate(channelid, channel.getIP());
        }

        bool canDeploy;

        wldLock.EnterWriteLock();    // thanks Ashen for noticing a deadlock issue when trying to deploy a channel
        try
        {
            canDeploy = world.getId() == worlds.Count;
            if (canDeploy)
            {
                _worldRecommendedList.Add(new(i, why_am_i_recommended));
                worlds.Add(world);
                channels.Insert(i, channelInfo);
            }
        }
        finally
        {
            wldLock.ExitWriteLock();
        }

        if (canDeploy)
        {
            world.setServerMessage(YamlConfig.config.worlds.get(i).server_message);

            log.Information("Finished loading world {WorldId}", i);
            return i;
        }
        else
        {
            log.Error("Could not load world {WorldId}...", i);
            world.shutdown();
            return -2;
        }
    }

    public bool removeChannel(int worldid)
    {   //lol don't!
        World world;

        wldLock.EnterReadLock();
        try
        {
            if (worldid >= worlds.Count)
            {
                return false;
            }
            world = worlds.get(worldid);
        }
        finally
        {
            wldLock.ExitReadLock();
        }

        if (world != null)
        {
            int channel = world.removeChannel();
            wldLock.EnterWriteLock();
            try
            {
                Dictionary<int, string> m = channels.get(worldid);
                if (m != null)
                {
                    m.Remove(channel);
                }
            }
            finally
            {
                wldLock.ExitWriteLock();
            }

            return channel > -1;
        }

        return false;
    }

    public bool removeWorld()
    {   //lol don't!
        World w;
        int worldid;

        wldLock.EnterReadLock();
        try
        {
            worldid = worlds.Count - 1;
            if (worldid < 0)
            {
                return false;
            }

            w = worlds.get(worldid);
        }
        finally
        {
            wldLock.ExitReadLock();
        }

        if (w == null || !w.canUninstall())
        {
            return false;
        }

        removeWorldPlayerRanking();
        w.shutdown();

        wldLock.EnterWriteLock();
        try
        {
            if (worldid == worlds.Count - 1)
            {
                worlds.remove(worldid);
                channels.remove(worldid);
                _worldRecommendedList.remove(worldid);
            }
        }
        finally
        {
            wldLock.ExitWriteLock();
        }

        return true;
    }

    private void resetServerWorlds()
    {  // thanks maple006 for noticing proprietary lists assigned to null
        wldLock.EnterWriteLock();
        try
        {
            worlds.Clear();
            channels.Clear();
            _worldRecommendedList.Clear();
        }
        finally
        {
            wldLock.ExitWriteLock();
        }
    }

    private static TimeSpan getTimeLeftForNextHour()
    {
        var nextHour = DateTimeOffset.Now.Date.AddHours(DateTimeOffset.Now.Hour + 1);
        return (nextHour - DateTimeOffset.Now);
    }

    public static TimeSpan getTimeLeftForNextDay()
    {
        return (DateTimeOffset.Now.AddDays(1).Date - DateTimeOffset.Now);
    }

    public Dictionary<int, int> getCouponRates()
    {
        return couponRates;
    }

    public static void cleanNxcodeCoupons(DBContext dbContext)
    {
        if (!YamlConfig.config.server.USE_CLEAR_OUTDATED_COUPONS)
        {
            return;
        }

        long timeClear = DateTimeOffset.Now.AddDays(-14).ToUnixTimeMilliseconds();

        using var dbTrans = dbContext.Database.BeginTransaction();
        var codeList = dbContext.Nxcodes.Where(x => x.Expiration <= timeClear).ToList();
        var codeIdList = codeList.Select(x => x.Id).ToList();
        dbContext.NxcodeItems.Where(x => codeIdList.Contains(x.Codeid)).ExecuteDelete();
        dbContext.Nxcodes.RemoveRange(codeList);
        dbContext.SaveChanges();
        dbTrans.Commit();
    }

    private void loadCouponRates(DBContext dbContext)
    {
        var list = dbContext.Nxcoupons.AsNoTracking().ToList();
        list.ForEach(rs =>
        {
            couponRates.AddOrUpdate(rs.CouponId, rs.Rate);
        });
    }

    public List<int> getActiveCoupons()
    {
        lock (activeCoupons)
        {
            return activeCoupons;
        }
    }

    public void commitActiveCoupons()
    {
        foreach (World world in getWorlds())
        {
            foreach (Character chr in world.getPlayerStorage().getAllCharacters())
            {
                if (!chr.isLoggedin())
                {
                    continue;
                }

                chr.updateCouponRates();
            }
        }
    }

    public void toggleCoupon(int couponId)
    {
        if (ItemConstants.isRateCoupon(couponId))
        {
            lock (activeCoupons)
            {
                if (activeCoupons.Contains(couponId))
                {
                    activeCoupons.Remove(couponId);
                }
                else
                {
                    activeCoupons.Add(couponId);
                }

                commitActiveCoupons();
            }
        }
    }

    public void updateActiveCoupons(DBContext dbContext)
    {
        lock (activeCoupons)
        {
            activeCoupons.Clear();
            var d = DateTimeOffset.Now;

            int weekDay = (int)d.DayOfWeek;
            int hourDay = d.Hour;

            int weekdayMask = (1 << weekDay);
            activeCoupons = dbContext.Nxcoupons.Where(x => x.Starthour <= hourDay && x.Endhour > hourDay && (x.Activeday & weekdayMask) == weekdayMask)
                    .Select(x => x.CouponId).ToList();

        }
    }

    public void runAnnouncePlayerDiseasesSchedule()
    {
        List<Client> processDiseaseAnnounceClients;
        Monitor.Enter(disLock);
        try
        {
            processDiseaseAnnounceClients = new(processDiseaseAnnouncePlayers);
            processDiseaseAnnouncePlayers.Clear();
        }
        finally
        {
            Monitor.Exit(disLock);
        }

        while (processDiseaseAnnounceClients.Count > 0)
        {
            Client c = processDiseaseAnnounceClients.remove(0);
            Character player = c.getPlayer();
            if (player != null && player.isLoggedinWorld())
            {
                player.announceDiseases();
                player.collectDiseases();
            }
        }

        Monitor.Enter(disLock);
        try
        {
            // this is to force the system to wait for at least one complete tick before releasing disease info for the registered clients
            while (registeredDiseaseAnnouncePlayers.Count > 0)
            {
                Client c = registeredDiseaseAnnouncePlayers.remove(0);
                processDiseaseAnnouncePlayers.Add(c);
            }
        }
        finally
        {
            Monitor.Exit(disLock);
        }
    }

    public void registerAnnouncePlayerDiseases(Client c)
    {
        Monitor.Enter(disLock);
        try
        {
            registeredDiseaseAnnouncePlayers.Add(c);
        }
        finally
        {
            Monitor.Exit(disLock);
        }
    }

    public List<KeyValuePair<string, int>> getWorldPlayerRanking(int worldid)
    {
        wldLock.EnterReadLock();
        try
        {
            return new(playerRanking.get(!YamlConfig.config.server.USE_WHOLE_SERVER_RANKING ? worldid : 0));
        }
        finally
        {
            wldLock.ExitReadLock();
        }
    }

    private void installWorldPlayerRanking(int worldid)
    {
        var ranking = loadPlayerRankingFromDB(worldid);
        if (ranking.Count > 0)
        {
            wldLock.EnterWriteLock();
            try
            {
                if (!YamlConfig.config.server.USE_WHOLE_SERVER_RANKING)
                {
                    for (int i = playerRanking.Count; i <= worldid; i++)
                    {
                        playerRanking.Add(new(0));
                    }

                    playerRanking.Insert(worldid, ranking.get(0).Value);
                }
                else
                {
                    playerRanking.Insert(0, ranking.get(0).Value);
                }
            }
            finally
            {
                wldLock.ExitWriteLock();
            }
        }
    }

    private void removeWorldPlayerRanking()
    {
        if (!YamlConfig.config.server.USE_WHOLE_SERVER_RANKING)
        {
            wldLock.EnterWriteLock();
            try
            {
                if (playerRanking.Count < worlds.Count)
                {
                    return;
                }

                playerRanking.RemoveAt(playerRanking.Count - 1);
            }
            finally
            {
                wldLock.ExitWriteLock();
            }
        }
        else
        {
            var ranking = loadPlayerRankingFromDB(-1 * (this.getWorldsSize() - 2));  // update ranking list

            wldLock.EnterWriteLock();
            try
            {
                playerRanking.Insert(0, ranking.get(0).Value);
            }
            finally
            {
                wldLock.ExitWriteLock();
            }
        }
    }

    public void updateWorldPlayerRanking()
    {
        var rankUpdates = loadPlayerRankingFromDB(-1 * (this.getWorldsSize() - 1));
        if (rankUpdates.Count == 0)
        {
            return;
        }

        wldLock.EnterWriteLock();
        try
        {
            if (!YamlConfig.config.server.USE_WHOLE_SERVER_RANKING)
            {
                for (int i = playerRanking.Count; i <= rankUpdates.get(rankUpdates.Count - 1).Key; i++)
                {
                    playerRanking.Add(new(0));
                }

                foreach (var wranks in rankUpdates)
                {
                    playerRanking.set(wranks.Key, wranks.Value);
                }
            }
            else
            {
                playerRanking.set(0, rankUpdates.get(0).Value);
            }
        }
        finally
        {
            wldLock.ExitWriteLock();
        }

    }

    private void initWorldPlayerRanking()
    {
        if (YamlConfig.config.server.USE_WHOLE_SERVER_RANKING)
        {
            wldLock.EnterWriteLock();
            try
            {
                playerRanking.Add(new(0));
            }
            finally
            {
                wldLock.ExitWriteLock();
            }
        }

        updateWorldPlayerRanking();
    }

    private static List<KeyValuePair<int, List<KeyValuePair<string, int>>>> loadPlayerRankingFromDB(int worldid)
    {
        List<KeyValuePair<int, List<KeyValuePair<string, int>>>> rankSystem = new List<KeyValuePair<int, List<KeyValuePair<string, int>>>>();
        List<KeyValuePair<string, int>> rankUpdate = new List<KeyValuePair<string, int>>(0);

        try
        {
            using var dbContext = new DBContext();
            var query = from a in dbContext.Characters
                        join b in dbContext.Accounts on a.AccountId equals b.Id
                        where a.Gm < 2 && b.Banned != 1
                        select a;

            if (!YamlConfig.config.server.USE_WHOLE_SERVER_RANKING)
            {
                if (worldid >= 0)
                {
                    query = query.Where(x => x.World == worldid);
                }
                else
                {
                    query = query.Where(x => x.World >= 0 && x.World <= -worldid);
                }
                query = query.OrderBy(x => x.World);
            }
            else
            {
                var absWorldId = Math.Abs(worldid);
                query = query.Where(x => x.World >= 0 && x.World <= absWorldId);
            }
            var list = (from a in query
                        orderby a.World, a.Level descending, a.Exp descending, a.LastExpGainTime
                        select new { a.Name, a.Level, a.World, }).Take(50).ToList();


            if (!YamlConfig.config.server.USE_WHOLE_SERVER_RANKING)
            {
                int currentWorld = -1;
                list.ForEach(x =>
                {
                    if (currentWorld < x.World)
                    {
                        currentWorld = x.World;
                        rankUpdate = new List<KeyValuePair<string, int>>(50);
                        rankSystem.Add(new(x.World, rankUpdate));
                    }
                    rankUpdate.Add(new(x.Name, x.Level));
                });
            }
            else
            {
                rankUpdate = new(50);
                rankSystem.Add(new(0, rankUpdate));
                list.ForEach(x =>
                {
                    rankUpdate.Add(new(x.Name, x.Level));
                });
            }
        }
        catch (Exception ex)
        {
            log.Error(ex.ToString());
        }

        return rankSystem;
    }

    public async Task init()
    {
        log.Information("Cosmic v{Version} starting up.", ServerConstants.VERSION);

        if (YamlConfig.config.server.SHUTDOWNHOOK)
        {
            AppDomain.CurrentDomain.ProcessExit += (obj, evt) => shutdown(false);
        }

        channelDependencies = registerChannelDependencies();

        Stopwatch sw = new Stopwatch();
        sw.Start();

        SkillFactory.loadAllSkills();
        sw.Stop();
        log.Debug($"Skills loaded in {sw.Elapsed.TotalSeconds} seconds");

        sw.Restart();
        CashItemFactory.loadAllCashItems();
        sw.Stop();
        log.Debug($"CashItems loaded in {sw.Elapsed.TotalSeconds} seconds");

        sw.Restart();
        Quest.loadAllQuests();
        sw.Stop();
        log.Debug($"Quest loaded in {sw.Elapsed.TotalSeconds} seconds");

        sw.Restart();
        SkillbookInformationProvider.loadAllSkillbookInformation();
        sw.Stop();
        log.Debug($"Skillbook loaded in {sw.Elapsed.TotalSeconds} seconds");

        int worldCount = Math.Min(GameConstants.WORLD_NAMES.Length, YamlConfig.config.server.WORLDS);

        sw.Restart();
        try
        {
            using var dbContext = new DBContext();
            setAllLoggedOut(dbContext);
            setAllMerchantsInactive(dbContext);
            cleanNxcodeCoupons(dbContext);
            loadCouponRates(dbContext);
            updateActiveCoupons(dbContext);
            NewYearCardRecord.startPendingNewYearCardRequests();
            CashIdGenerator.loadExistentCashIdsFromDb(dbContext);
            applyAllNameChanges(dbContext); // -- name changes can be missed by INSTANT_NAME_CHANGE --
            applyAllWorldTransfers(dbContext);
            PlayerNPC.loadRunningRankData(dbContext, worldCount);
        }
        catch (Exception sqle)
        {
            log.Error(sqle, "Failed to run all startup-bound database tasks");
            throw;
        }

        ThreadManager.getInstance().start();
        await initializeTimelyTasks(channelDependencies);    // aggregated method for timely tasks thanks to lxconan

        try
        {
            for (int i = 0; i < worldCount; i++)
            {
                initWorld();
            }
            initWorldPlayerRanking();

            loadPlayerNpcMapStepFromDb();

            if (YamlConfig.config.server.USE_FAMILY_SYSTEM)
            {
                using var dbContext = new DBContext();
                Family.loadAllFamilies(dbContext);
            }
        }
        catch (Exception e)
        {
            log.Error(e, "[SEVERE] Syntax error in 'world.ini'."); //For those who get errors
            Environment.Exit(0);
        }

        // Wait on all async tasks to complete

        loginServer = await initLoginServer(8484);

        log.Information("Listening on port 8484");

        online = true;
        sw.Stop();
        log.Information("Cosmic is now online after {Startup} s.", sw.Elapsed.TotalSeconds);

        OpcodeConstants.generateOpcodeNames();
        CommandsExecutor.getInstance();

        foreach (Channel ch in this.getAllChannels())
        {
            ch.reloadEventScriptManager();
        }
        await Task.Delay(Timeout.Infinite);
    }

    private ChannelDependencies registerChannelDependencies()
    {
        NoteService noteService = new NoteService(new NoteDao());
        FredrickProcessor fredrickProcessor = new FredrickProcessor(noteService);
        ChannelDependencies channelDependencies = new ChannelDependencies(noteService, fredrickProcessor);

        PacketProcessor.registerGameHandlerDependencies(channelDependencies);

        return channelDependencies;
    }

    private async Task<LoginServer> initLoginServer(int port)
    {
        LoginServer loginServer = new LoginServer(port);
        await loginServer.Start();
        return loginServer;
    }

    private static void setAllLoggedOut(DBContext dbContext)
    {

        dbContext.Accounts.ExecuteUpdate(x => x.SetProperty(y => y.Loggedin, 0));
    }

    private static void setAllMerchantsInactive(DBContext dbContext)
    {
        dbContext.Characters.ExecuteUpdate(x => x.SetProperty(y => y.HasMerchant, false));
    }

    private async Task initializeTimelyTasks(ChannelDependencies channelDependencies)
    {
        TimerManager tMan = TimerManager.getInstance();
        await tMan.start();
        tMan.register(tMan.purge, YamlConfig.config.server.PURGING_INTERVAL);//Purging ftw...
        disconnectIdlesOnLoginTask();

        var timeLeft = getTimeLeftForNextHour();
        tMan.register(new CharacterDiseaseTask(), YamlConfig.config.server.UPDATE_INTERVAL, YamlConfig.config.server.UPDATE_INTERVAL);
        tMan.register(new CouponTask(), YamlConfig.config.server.COUPON_INTERVAL, (long)timeLeft.TotalMilliseconds);
        tMan.register(new RankingCommandTask(), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        tMan.register(new RankingLoginTask(), YamlConfig.config.server.RANKING_INTERVAL, (long)timeLeft.TotalMilliseconds);
        tMan.register(new LoginCoordinatorTask(), TimeSpan.FromHours(1), timeLeft);
        tMan.register(new EventRecallCoordinatorTask(), TimeSpan.FromHours(1), timeLeft);
        tMan.register(new LoginStorageTask(), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
        tMan.register(new DueyFredrickTask(channelDependencies.fredrickProcessor), TimeSpan.FromHours(1), timeLeft);
        tMan.register(new InvitationTask(), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        tMan.register(new RespawnTask(), YamlConfig.config.server.RESPAWN_INTERVAL, YamlConfig.config.server.RESPAWN_INTERVAL);

        timeLeft = getTimeLeftForNextDay();
        ExpeditionBossLog.resetBossLogTable();
        tMan.register(new BossLogTask(), TimeSpan.FromDays(1), timeLeft);
    }

    public Dictionary<string, string> getSubnetInfo()
    {
        return subnetInfo;
    }

    public Alliance? getAlliance(int id)
    {
        lock (alliances)
        {
            return alliances.GetValueOrDefault(id);
        }
    }

    public void addAlliance(int id, Alliance alliance)
    {
        lock (alliances)
        {
            if (!alliances.ContainsKey(id))
            {
                alliances.Add(id, alliance);
            }
        }
    }

    public void disbandAlliance(int id)
    {
        lock (alliances)
        {
            Alliance? alliance = alliances.GetValueOrDefault(id);
            if (alliance != null)
            {
                foreach (int gid in alliance.getGuilds())
                {
                    guilds.GetValueOrDefault(gid)!.setAllianceId(0);
                }
                alliances.Remove(id);
            }
        }
    }

    public void allianceMessage(int id, Packet packet, int exception, int guildex)
    {
        var alliance = alliances.GetValueOrDefault(id);
        if (alliance != null)
        {
            foreach (int gid in alliance.getGuilds())
            {
                if (guildex == gid)
                {
                    continue;
                }
                var guild = guilds.GetValueOrDefault(gid);
                if (guild != null)
                {
                    guild.broadcast(packet, exception);
                }
            }
        }
    }

    public bool addGuildtoAlliance(int aId, int guildId)
    {
        Alliance? alliance = alliances.GetValueOrDefault(aId);
        if (alliance != null)
        {
            alliance.addGuild(guildId);
            guilds.GetValueOrDefault(guildId)!.setAllianceId(aId);
            return true;
        }
        return false;
    }

    public bool removeGuildFromAlliance(int aId, int guildId)
    {
        Alliance? alliance = alliances.GetValueOrDefault(aId);
        if (alliance != null)
        {
            alliance.removeGuild(guildId);
            guilds.GetValueOrDefault(guildId)!.setAllianceId(0);
            return true;
        }
        return false;
    }

    public bool setAllianceRanks(int aId, string[] ranks)
    {
        Alliance? alliance = alliances.GetValueOrDefault(aId);
        if (alliance != null)
        {
            alliance.setRankTitle(ranks);
            return true;
        }
        return false;
    }

    public bool setAllianceNotice(int aId, string notice)
    {
        Alliance? alliance = alliances.GetValueOrDefault(aId);
        if (alliance != null)
        {
            alliance.setNotice(notice);
            return true;
        }
        return false;
    }

    public bool increaseAllianceCapacity(int aId, int inc)
    {
        Alliance? alliance = alliances.GetValueOrDefault(aId);
        if (alliance != null)
        {
            alliance.increaseCapacity(inc);
            return true;
        }
        return false;
    }

    public int createGuild(int leaderId, string name)
    {
        return Guild.createGuild(leaderId, name);
    }

    public Guild? getGuildByName(string name)
    {
        lock (guilds)
        {
            foreach (Guild mg in guilds.Values)
            {
                if (mg.getName().Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return mg;
                }
            }

            return null;
        }
    }

    public Guild? getGuild(int id)
    {
        lock (guilds)
        {
            return guilds.GetValueOrDefault(id);
        }
    }

    public Guild? getGuild(int id, int world)
    {
        return getGuild(id, world, null);
    }

    public Guild? getGuild(int id, int world, Character? mc)
    {
        lock (guilds)
        {
            Guild? g = guilds.GetValueOrDefault(id);
            if (g != null)
            {
                return g;
            }

            g = new Guild(id, world);
            if (g.getId() == -1)
            {
                return null;
            }

            if (mc != null)
            {
                var mgc = g.getMGC(mc.getId());
                if (mgc != null)
                {
                    mc.setMGC(mgc);
                    mgc.setCharacter(mc);
                }
                else
                {
                    log.Error("Could not find chr {CharacterName} when loading guild {GuildId}", mc.getName(), id);
                }

                g.setOnline(mc.getId(), true, mc.getClient().getChannel());
            }

            guilds.AddOrUpdate(id, g);
            return g;
        }
    }

    public void setGuildMemberOnline(Character mc, bool bOnline, int channel)
    {
        Guild? g = getGuild(mc.getGuildId(), mc.getWorld(), mc);
        g.setOnline(mc.getId(), bOnline, channel);
    }

    public int addGuildMember(GuildCharacter mgc, Character chr)
    {
        Guild? g = guilds.GetValueOrDefault(mgc.getGuildId());
        if (g != null)
        {
            return g.addGuildMember(mgc, chr);
        }
        return 0;
    }

    public bool setGuildAllianceId(int gId, int aId)
    {
        Guild? guild = guilds.GetValueOrDefault(gId);
        if (guild != null)
        {
            guild.setAllianceId(aId);
            return true;
        }
        return false;
    }

    public void resetAllianceGuildPlayersRank(int gId)
    {
        guilds.GetValueOrDefault(gId)?.resetAllianceGuildPlayersRank();
    }

    public void leaveGuild(GuildCharacter mgc)
    {
        Guild? g = guilds.GetValueOrDefault(mgc.getGuildId());
        if (g != null)
        {
            g.leaveGuild(mgc);
        }
    }

    public void guildChat(int gid, string name, int cid, string msg)
    {
        Guild? g = guilds.GetValueOrDefault(gid);
        if (g != null)
        {
            g.guildChat(name, cid, msg);
        }
    }

    public void changeRank(int gid, int cid, int newRank)
    {
        Guild? g = guilds.GetValueOrDefault(gid);
        if (g != null)
        {
            g.changeRank(cid, newRank);
        }
    }

    public void expelMember(GuildCharacter initiator, string name, int cid)
    {
        Guild? g = guilds.GetValueOrDefault(initiator.getGuildId());
        if (g != null)
        {
            g.expelMember(initiator, name, cid, channelDependencies.noteService);
        }
    }

    public void setGuildNotice(int gid, string notice)
    {
        Guild? g = guilds.GetValueOrDefault(gid);
        if (g != null)
        {
            g.setGuildNotice(notice);
        }
    }

    public void memberLevelJobUpdate(GuildCharacter mgc)
    {
        Guild? g = guilds.GetValueOrDefault(mgc.getGuildId());
        if (g != null)
        {
            g.memberLevelJobUpdate(mgc);
        }
    }

    public void changeRankTitle(int gid, string[] ranks)
    {
        Guild? g = guilds.GetValueOrDefault(gid);
        if (g != null)
        {
            g.changeRankTitle(ranks);
        }
    }

    public void setGuildEmblem(int gid, short bg, byte bgcolor, short logo, byte logocolor)
    {
        Guild? g = guilds.GetValueOrDefault(gid);
        if (g != null)
        {
            g.setGuildEmblem(bg, bgcolor, logo, logocolor);
        }
    }

    public void disbandGuild(int gid)
    {
        lock (guilds)
        {
            Guild g = guilds.GetValueOrDefault(gid);
            g.disbandGuild();
            guilds.Remove(gid);
        }
    }

    public bool increaseGuildCapacity(int gid)
    {
        Guild? g = guilds.GetValueOrDefault(gid);
        if (g != null)
        {
            return g.increaseCapacity();
        }
        return false;
    }

    public void gainGP(int gid, int amount)
    {
        Guild? g = guilds.GetValueOrDefault(gid);
        if (g != null)
        {
            g.gainGP(amount);
        }
    }

    public void guildMessage(int gid, Packet packet)
    {
        guildMessage(gid, packet, -1);
    }

    public void guildMessage(int gid, Packet packet, int exception)
    {
        Guild? g = guilds.GetValueOrDefault(gid);
        if (g != null)
        {
            g.broadcast(packet, exception);
        }
    }

    public PlayerBuffStorage getPlayerBuffStorage()
    {
        return buffStorage;
    }

    public void deleteGuildCharacter(Character mc)
    {
        setGuildMemberOnline(mc, false, -1);
        if (mc.getMGC().getGuildRank() > 1)
        {
            leaveGuild(mc.getMGC());
        }
        else
        {
            disbandGuild(mc.getMGC().getGuildId());
        }
    }

    public void deleteGuildCharacter(GuildCharacter mgc)
    {
        if (mgc.getCharacter() != null)
        {
            setGuildMemberOnline(mgc.getCharacter(), false, -1);
        }
        if (mgc.getGuildRank() > 1)
        {
            leaveGuild(mgc);
        }
        else
        {
            disbandGuild(mgc.getGuildId());
        }
    }

    public void reloadGuildCharacters(int world)
    {
        var worlda = getWorld(world);
        foreach (Character mc in worlda.getPlayerStorage().getAllCharacters())
        {
            if (mc.getGuildId() > 0)
            {
                setGuildMemberOnline(mc, true, worlda.getId());
                memberLevelJobUpdate(mc.getMGC());
            }
        }
        worlda.reloadGuildSummary();
    }

    public void broadcastMessage(int world, Packet packet)
    {
        foreach (Channel ch in getChannelsFromWorld(world))
        {
            ch.broadcastPacket(packet);
        }
    }

    public void broadcastGMMessage(int world, Packet packet)
    {
        foreach (Channel ch in getChannelsFromWorld(world))
        {
            ch.broadcastGMPacket(packet);
        }
    }

    public bool isGmOnline(int world)
    {
        foreach (Channel ch in getChannelsFromWorld(world))
        {
            foreach (Character player in ch.getPlayerStorage().getAllCharacters())
            {
                if (player.isGM())
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void changeFly(int accountid, bool canFly)
    {
        if (canFly)
        {
            activeFly.Add(accountid);
        }
        else
        {
            activeFly.Remove(accountid);
        }
    }

    public bool canFly(int accountid)
    {
        return activeFly.Contains(accountid);
    }

    public int getCharacterWorld(int chrid)
    {
        lgnLock.EnterReadLock();
        try
        {
            return worldChars.GetValueOrDefault(chrid, -1);
        }
        finally
        {
            lgnLock.ExitReadLock();
        }
    }

    public bool haveCharacterEntry(int accountid, int chrid)
    {
        lgnLock.EnterReadLock();
        try
        {
            HashSet<int> accChars = accountChars.GetValueOrDefault(accountid);
            return accChars.Contains(chrid);
        }
        finally
        {
            lgnLock.ExitReadLock();
        }
    }

    public short getAccountCharacterCount(int accountid)
    {
        lgnLock.EnterReadLock();
        try
        {
            return accountCharacterCount.GetValueOrDefault(accountid);
        }
        finally
        {
            lgnLock.ExitReadLock();
        }
    }

    public short getAccountWorldCharacterCount(int accountid, int worldid)
    {
        lgnLock.EnterReadLock();
        try
        {
            short count = 0;

            foreach (int chr in accountChars.GetValueOrDefault(accountid))
            {
                if (worldChars.get(chr).Equals(worldid))
                {
                    count++;
                }
            }

            return count;
        }
        finally
        {
            lgnLock.ExitReadLock();
        }
    }

    private HashSet<int> getAccountCharacterEntries(int accountid)
    {
        lgnLock.EnterReadLock();
        try
        {
            return new(accountChars.GetValueOrDefault(accountid));
        }
        finally
        {
            lgnLock.ExitReadLock();
        }
    }

    public void updateCharacterEntry(Character chr)
    {
        Character chrView = chr.generateCharacterEntry();

        lgnLock.EnterWriteLock();
        try
        {
            var wserv = this.getWorld(chrView.getWorld());
            if (wserv != null)
            {
                wserv.registerAccountCharacterView(chrView.getAccountID(), chrView);
            }
        }
        finally
        {
            lgnLock.ExitWriteLock();
        }
    }

    public void createCharacterEntry(Character chr)
    {
        int accountid = chr.getAccountID(), chrid = chr.getId(), world = chr.getWorld();

        lgnLock.EnterWriteLock();
        try
        {
            accountCharacterCount.AddOrUpdate(accountid, (short)(accountCharacterCount.get(accountid) + 1));

            var accChars = accountChars.GetValueOrDefault(accountid);
            accChars.Add(chrid);

            worldChars.AddOrUpdate(chrid, world);

            Character chrView = chr.generateCharacterEntry();

            var wserv = this.getWorld(chrView.getWorld());
            if (wserv != null)
            {
                wserv.registerAccountCharacterView(chrView.getAccountID(), chrView);
            }
        }
        finally
        {
            lgnLock.ExitWriteLock();
        }
    }

    public void deleteCharacterEntry(int accountid, int chrid)
    {
        lgnLock.EnterWriteLock();
        try
        {
            accountCharacterCount.AddOrUpdate(accountid, (short)(accountCharacterCount.get(accountid) - 1));

            var accChars = accountChars.GetValueOrDefault(accountid);
            accChars?.Remove(chrid);

            if (worldChars.Remove(chrid, out var world))
            {
                var wserv = this.getWorld(world);
                if (wserv != null)
                {
                    wserv.unregisterAccountCharacterView(accountid, chrid);
                }
            }
        }
        finally
        {
            lgnLock.ExitWriteLock();
        }
    }

    public void transferWorldCharacterEntry(Character chr, int toWorld)
    { // used before setting the new worldid on the character object
        lgnLock.EnterWriteLock();
        try
        {
            int chrid = chr.getId(), accountid = chr.getAccountID();
            var world = worldChars.get(chr.getId());
            if (world != null)
            {
                var wservTmp = this.getWorld(world.Value);
                if (wservTmp != null)
                {
                    wservTmp.unregisterAccountCharacterView(accountid, chrid);
                }
            }

            worldChars.AddOrUpdate(chrid, toWorld);

            Character chrView = chr.generateCharacterEntry();

            var wserv = this.getWorld(toWorld);
            if (wserv != null)
            {
                wserv.registerAccountCharacterView(chrView.getAccountID(), chrView);
            }
        }
        finally
        {
            lgnLock.ExitWriteLock();
        }
    }

    /*
    public void deleteAccountEntry(int accountid) { is this even a thing?
        lgnLock.EnterWriteLock();
        try {
            accountCharacterCount.Remove(accountid);
            accountChars.Remove(accountid);
        } finally {
            lgnLock.ExitWriteLock();
        }

        foreach(World wserv in this.getWorlds()) {
            wserv.clearAccountCharacterView(accountid);
            wserv.unregisterAccountStorage(accountid);
        }
    }
    */

    public SortedDictionary<int, List<Character>> loadAccountCharlist(int accountId, int visibleWorlds)
    {
        List<World> worlds = this.getWorlds();
        if (worlds.Count > visibleWorlds)
        {
            worlds = worlds.Take(visibleWorlds).ToList();
        }

        SortedDictionary<int, List<Character>> worldChrs = new();
        int chrTotal = 0;

        lgnLock.EnterReadLock();
        try
        {
            foreach (World world in worlds)
            {
                var chrs = world.getAccountCharactersView(accountId);
                if (chrs == null)
                {
                    if (!accountChars.ContainsKey(accountId))
                    {
                        accountCharacterCount.AddOrUpdate(accountId, (short)0);
                        accountChars.AddOrUpdate(accountId, new());    // not advisable at all to write on the map on a read-protected environment
                    }                                                           // yet it's known there's no problem since no other point in the source does
                }
                else if (chrs.Count > 0)
                {                                  // this action.
                    worldChrs.AddOrUpdate(world.getId(), chrs);
                }
            }
        }
        finally
        {
            lgnLock.ExitReadLock();
        }

        return worldChrs;
    }

    private static KeyValuePair<short, List<List<Character>>> loadAccountCharactersViewFromDb(int accId, int wlen)
    {
        short characterCount = 0;
        List<List<Character>> wchars = new(wlen);
        for (int i = 0; i < wlen; i++)
        {
            wchars.Insert(i, new());
        }

        List<Character> chars = new();
        int curWorld = 0;
        try
        {
            var accEquips = ItemFactory.loadEquippedItems(accId, true, true);
            Dictionary<int, List<Item>> accPlayerEquips = new();

            foreach (var ae in accEquips)
            {
                var playerEquips = accPlayerEquips.GetValueOrDefault(ae.Value);
                if (playerEquips == null)
                {
                    playerEquips = new();
                    accPlayerEquips.AddOrUpdate(ae.Value, playerEquips);
                }

                playerEquips.Add(ae.Key);
            }


            using var dbContext = new DBContext();
            var charsFromDb = dbContext.Characters.Where(x => x.AccountId == accId).OrderBy(x => x.World).ToList();
            charsFromDb.ForEach(x =>
            {
                if (x.World >= wlen)
                    return;

                if (x.World > curWorld)
                {
                    wchars.Insert(curWorld, chars);
                    curWorld = x.World;
                    chars = new();
                }

                chars.Add(Character.loadCharacterEntryFromDB(x, accPlayerEquips.GetValueOrDefault(x.Id)));
            });
        }
        catch (Exception sqle)
        {
            log.Error(sqle.ToString());
        }

        return new(characterCount, wchars);
    }

    public void loadAllAccountsCharactersView()
    {
        using var dbContext = new DBContext();
        var idList = dbContext.Accounts.Select(x => x.Id).ToList();
        idList.ForEach(accountId =>
        {
            if (isFirstAccountLogin(accountId))
            {
                loadAccountCharactersView(accountId, 0, 0);
            }
        });
    }

    private bool isFirstAccountLogin(int accId)
    {
        lgnLock.EnterReadLock();
        try
        {
            return !accountChars.ContainsKey(accId);
        }
        finally
        {
            lgnLock.ExitReadLock();
        }
    }

    private static void applyAllNameChanges(DBContext dbContext)
    {
        try
        {
            List<NameChangePair> changedNames = new();
            using var dbTrans = dbContext.Database.BeginTransaction();
            var allChanges = dbContext.Namechanges.Where(x => x.CompletionTime == null).ToList();
            allChanges.ForEach(x =>
            {
                bool success = Character.doNameChange(dbContext, x.Characterid, x.Old, x.New, x.Id);
                if (!success)
                    dbTrans.Rollback();
                else
                {
                    dbTrans.Commit();
                    changedNames.Add(new(x.Old, x.New));
                }
            });

            //log
            foreach (var namePair in changedNames)
            {
                log.Information("Name change applied - from: \"{CharacterName}\" to \"{CharacterName}\"", namePair.OldName, namePair.NewName);
            }
        }
        catch (Exception e)
        {
            log.Warning(e, "Failed to retrieve list of pending name changes");
            throw;
        }
    }

    private static void applyAllWorldTransfers(DBContext dbContext)
    {
        try
        {
            var ds = dbContext.Worldtransfers.Where(x => x.CompletionTime == null).ToList();
            List<int> removedTransfers = new();

            ds.ForEach(x =>
            {
                string? reason = Character.checkWorldTransferEligibility(dbContext, x.Characterid, x.From, x.To);
                if (!string.IsNullOrEmpty(reason))
                {
                    removedTransfers.Add(x.Id);
                    dbContext.Worldtransfers.Remove(x);
                    log.Information("World transfer canceled: chrId {CharacterId}, reason {WorldTransferReason}", x.Characterid, reason);
                }
            });

            using var dbTrans = dbContext.Database.BeginTransaction();
            List<CharacterWorldTransferPair> worldTransfers = new(); //logging only <charid, <oldWorld, newWorld>>

            ds.ForEach(x =>
            {
                var success = Character.doWorldTransfer(dbContext, x.Characterid, x.From, x.To, x.Id);
                if (!success)
                    dbTrans.Rollback();
                else
                {
                    dbTrans.Commit();
                    worldTransfers.Add(new(x.Characterid, x.From, x.To));
                }
            });

            //log
            foreach (var worldTransferPair in worldTransfers)
            {
                int charId = worldTransferPair.CharacterId;
                int oldWorld = worldTransferPair.OldId;
                int newWorld = worldTransferPair.NewId;
                log.Information("World transfer applied - character id {CharacterId} from world {WorldId} to world {WorldId}", charId, oldWorld, newWorld);
            }
        }
        catch (Exception e)
        {
            log.Warning(e, "Failed to retrieve list of pending world transfers");
            throw;
        }
    }

    public void loadAccountCharacters(Client c)
    {
        int accId = c.getAccID();
        if (!isFirstAccountLogin(accId))
        {
            HashSet<int> accWorlds = new();

            lgnLock.EnterReadLock();
            try
            {
                foreach (int chrid in getAccountCharacterEntries(accId))
                {
                    accWorlds.Add(worldChars.GetValueOrDefault(chrid));
                }
            }
            finally
            {
                lgnLock.ExitReadLock();
            }

            int gmLevelTemp = 0;
            foreach (int aw in accWorlds)
            {
                var wserv = this.getWorld(aw);

                if (wserv != null)
                {
                    foreach (Character chr in wserv.getAllCharactersView())
                    {
                        if (gmLevelTemp < chr.gmLevel())
                        {
                            gmLevelTemp = chr.gmLevel();
                        }
                    }
                }
            }

            c.setGMLevel(gmLevelTemp);
            return;
        }

        int gmLevel = loadAccountCharactersView(c.getAccID(), 0, 0);
        c.setGMLevel(gmLevel);
    }

    private int loadAccountCharactersView(int accId, int gmLevel, int fromWorldid)
    {    // returns the maximum gmLevel found
        List<World> wlist = this.getWorlds();
        var accCharacters = loadAccountCharactersViewFromDb(accId, wlist.Count);

        lgnLock.EnterWriteLock();
        try
        {
            List<List<Character>> accChars = accCharacters.Value;
            accountCharacterCount.AddOrUpdate(accId, accCharacters.Key);

            HashSet<int>? chars = accountChars.GetValueOrDefault(accId);
            if (chars == null)
            {
                chars = new(5);
            }

            for (int wid = fromWorldid; wid < wlist.Count; wid++)
            {
                World w = wlist.get(wid);
                List<Character> wchars = accChars.get(wid);
                w.loadAccountCharactersView(accId, wchars);

                foreach (Character chr in wchars)
                {
                    int cid = chr.getId();
                    if (gmLevel < chr.gmLevel())
                    {
                        gmLevel = chr.gmLevel();
                    }

                    chars.Add(cid);
                    worldChars.AddOrUpdate(cid, wid);
                }
            }

            accountChars.AddOrUpdate(accId, chars);
        }
        finally
        {
            lgnLock.ExitWriteLock();
        }

        return gmLevel;
    }

    public void loadAccountStorages(Client c)
    {
        int accountId = c.getAccID();
        HashSet<int> accWorlds = new();
        lgnLock.EnterWriteLock();
        try
        {
            var chars = accountChars.GetValueOrDefault(accountId);

            foreach (int cid in chars)
            {
                var worldid = worldChars.get(cid);
                if (worldid != null)
                {
                    accWorlds.Add(worldid.Value);
                }
            }
        }
        finally
        {
            lgnLock.ExitWriteLock();
        }

        List<World> worldList = this.getWorlds();
        foreach (int worldid in accWorlds)
        {
            if (worldid < worldList.Count)
            {
                World wserv = worldList.get(worldid);
                wserv.loadAccountStorage(accountId);
            }
        }
    }

    private static string getRemoteHost(Client client)
    {
        return SessionCoordinator.getSessionRemoteHost(client);
    }

    public void setCharacteridInTransition(Client client, int charId)
    {
        string remoteIp = getRemoteHost(client);

        lgnLock.EnterWriteLock();
        try
        {
            transitioningChars.AddOrUpdate(remoteIp, charId);
        }
        finally
        {
            lgnLock.ExitWriteLock();
        }
    }

    public bool validateCharacteridInTransition(Client client, int charId)
    {
        if (!YamlConfig.config.server.USE_IP_VALIDATION)
        {
            return true;
        }

        string remoteIp = getRemoteHost(client);

        lgnLock.EnterWriteLock();
        try
        {
            return transitioningChars.Remove(remoteIp, out var cid) && cid == charId;
        }
        finally
        {
            lgnLock.ExitWriteLock();
        }
    }

    public int? freeCharacteridInTransition(Client client)
    {
        if (!YamlConfig.config.server.USE_IP_VALIDATION)
        {
            return null;
        }

        string remoteIp = getRemoteHost(client);

        lgnLock.EnterWriteLock();
        try
        {
            if (transitioningChars.Remove(remoteIp, out var d))
                return d;
            return null;
        }
        finally
        {
            lgnLock.ExitWriteLock();
        }
    }

    public bool hasCharacteridInTransition(Client client)
    {
        if (!YamlConfig.config.server.USE_IP_VALIDATION)
        {
            return true;
        }

        string remoteIp = getRemoteHost(client);

        lgnLock.EnterReadLock();
        try
        {
            return transitioningChars.ContainsKey(remoteIp);
        }
        finally
        {
            lgnLock.ExitReadLock();
        }
    }

    public void registerLoginState(Client c)
    {
        Monitor.Enter(srvLock);
        try
        {
            inLoginState.AddOrUpdate(c, DateTimeOffset.Now.AddMinutes(1).ToUnixTimeMilliseconds());
        }
        finally
        {
            Monitor.Exit(srvLock);
        }
    }

    public void unregisterLoginState(Client c)
    {
        Monitor.Enter(srvLock);
        try
        {
            inLoginState.Remove(c);
        }
        finally
        {
            Monitor.Exit(srvLock);
        }
    }

    private void disconnectIdlesOnLoginState()
    {
        List<Client> toDisconnect = new();

        Monitor.Enter(srvLock);
        try
        {
            long timeNow = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            foreach (var mc in inLoginState)
            {
                if (timeNow > mc.Value)
                {
                    toDisconnect.Add(mc.Key);
                }
            }

            foreach (Client c in toDisconnect)
            {
                inLoginState.Remove(c);
            }
        }
        finally
        {
            Monitor.Exit(srvLock);
        }

        foreach (Client c in toDisconnect)
        {    // thanks Lei for pointing a deadlock issue with srvLock
            if (c.isLoggedIn())
            {
                c.disconnect(false, false);
            }
            else
            {
                SessionCoordinator.getInstance().closeSession(c, true);
            }
        }
    }

    private void disconnectIdlesOnLoginTask()
    {
        TimerManager.getInstance().register(() => disconnectIdlesOnLoginState(), 300000);
    }

    public Action shutdown(bool restart)
    {//no player should be online when trying to shutdown!
        return () => shutdownInternal(restart);
    }

    //synchronized
    private async void shutdownInternal(bool restart)
    {
        log.Information("{0} the server!", restart ? "Restarting" : "Shutting down");
        if (getWorlds() == null)
        {
            return;//already shutdown
        }
        foreach (World w in getWorlds())
        {
            w.shutdown();
        }

        /*foreach(World w in getWorlds()) {
            while (w.getPlayerStorage().getAllCharacters().Count > 0) {
                try {
                    Thread.sleep(1000);
                } catch (ThreadInterruptedException ie) {
                    System.err.println("FUCK MY LIFE");
                }
            }
        }
        foreach(Channel ch in getAllChannels()) {
            while (ch.getConnectedClients() > 0) {
                try {
                    Thread.sleep(1000);
                } catch (ThreadInterruptedException ie) {
                    System.err.println("FUCK MY LIFE");
                }
            }
        }*/

        List<Channel> allChannels = getAllChannels();

        foreach (Channel ch in allChannels)
        {
            while (!ch.finishedShutdown())
            {
                try
                {
                    Thread.Sleep(1000);
                }
                catch (ThreadInterruptedException ie)
                {
                    log.Error(ie, "Error during shutdown sleep");
                }
            }
        }

        resetServerWorlds();

        ThreadManager.getInstance().stop();
        TimerManager.getInstance().purge();
        TimerManager.getInstance().stop();

        log.Information("Worlds and channels are offline.");
        await loginServer.Stop();
        if (!restart)
        {  // shutdown hook deadlocks if System.exit() method is used within its body chores, thanks MIKE for pointing that out
            // We disabled log4j's shutdown hook in the config file, so we have to manually shut it down here,
            // after our last log statement.
            await Log.CloseAndFlushAsync();
            new Thread(() => Environment.Exit(0)).Start();
        }
        else
        {
            log.Information("Restarting the server...");
            instance = null;
            await getInstance().init();//DID I DO EVERYTHING?! D:
        }
    }
}
