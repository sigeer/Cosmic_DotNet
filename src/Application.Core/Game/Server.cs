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


using Application.Core.Game.Commands;
using Application.Core.Game.Life;
using Application.Core.Game.Relation;
using Application.Core.Game.TheWorld;
using Application.Core.Managers;
using Application.Core.model;
using client;
using client.inventory.manipulator;
using client.newyear;
using client.processor.npc;
using constants.game;
using constants.inventory;
using constants.net;
using database.note;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using net.netty;
using net.packet;
using net.server.channel;
using net.server.coordinator.session;
using net.server.task;
using server;
using server.expeditions;
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

    private LoginServer loginServer = null!;
    /// <summary>
    /// ChannelId - IP
    /// </summary>
    private List<Dictionary<int, string>> channelInfoList = new();
    private List<IWorld> worlds = new();
    private Dictionary<string, string> subnetInfo = new();

    /// <summary>
    /// AccountId - world - cid
    /// </summary>
    private Dictionary<int, HashSet<int>> AccountCharacterCache = new();

    private Dictionary<string, int> transitioningChars = new();
    private List<KeyValuePair<int, string>> _worldRecommendedList = new();
    private Dictionary<int, IGuild> guilds = new(100);
    private Dictionary<IClient, long> inLoginState = new(100);

    private PlayerBuffStorage buffStorage = new PlayerBuffStorage();
    private Dictionary<int, Alliance> alliances = new(100);
    private Dictionary<int, NewYearCardRecord> newyears = new();
    private List<IClient> processDiseaseAnnouncePlayers = new();
    private List<IClient> registeredDiseaseAnnouncePlayers = new();

    private List<List<NameLevelPair>> playerRanking = new();

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
        return (int)(getCurrentTime() - Server.uptime.ToUnixTimeMilliseconds());
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
        var wlist = this.getWorlds();

        using var dbContext = new DBContext();
        var list = dbContext.PlayernpcsFields.AsNoTracking().ToList();
        list.ForEach(rs =>
        {
            var w = wlist.FirstOrDefault(x => x.getId() == rs.World);
            if (w != null)
                w.setPlayerNpcMapData(rs.Map, rs.Step, rs.Podium);
        });
    }

    public IWorld getWorld(int id)
    {
        wldLock.EnterReadLock();
        try
        {
            return worlds.ElementAtOrDefault(id) ?? throw new BusinessException($"World {id} not exsited");
        }
        finally
        {
            wldLock.ExitReadLock();
        }
    }

    public List<IWorld> getWorlds()
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

    public IWorldChannel getChannel(int world, int channel)
    {
        return this.getWorld(world).getChannel(channel);
    }

    public List<IWorldChannel> getChannelsFromWorld(int world)
    {
        return this.getWorld(world).getChannels();
    }

    public List<IWorldChannel> getAllChannels()
    {
        try
        {
            List<IWorldChannel> channelz = new();
            foreach (var world in this.getWorlds())
            {
                channelz.AddRange(world.getChannels());
            }
            return channelz;
        }
        catch (NullReferenceException)
        {
            return new(0);
        }
    }

    public HashSet<int> getOpenChannels(int world)
    {
        wldLock.EnterReadLock();
        try
        {
            return new(channelInfoList.get(world).Keys);
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
            return channelInfoList.get(world).GetValueOrDefault(channel);
        }
        finally
        {
            wldLock.ExitReadLock();
        }
    }

    public string[] getInetSocket(IClient client, int world, int channel)
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

        return hostAddress;
    }


    private void dumpData()
    {
        wldLock.EnterReadLock();
        try
        {
            log.Debug("Worlds: {Worlds}", worlds);
            log.Debug("Channels: {Channels}", channelInfoList);
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
        IWorld? world;
        Dictionary<int, string> channelInfo;
        int channelid;

        wldLock.EnterReadLock();
        try
        {
            if (worldid >= worlds.Count)
            {
                return -3;
            }

            channelInfo = channelInfoList.get(worldid);
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

        IWorldChannel channel = new WorldChannel(world, channelid, getCurrentTime());
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
                accounts = new(AccountCharacterCache.Keys);
            }
            finally
            {
                lgnLock.ExitReadLock();
            }

            foreach (int accId in accounts)
            {
                LoadAccountCharactersView(accId, newWorld);
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

        var worldConfig = YamlConfig.config.worlds[i];
        int exprate = worldConfig.exp_rate;
        int mesorate = worldConfig.meso_rate;
        int droprate = worldConfig.drop_rate;
        int bossdroprate = worldConfig.boss_drop_rate;
        int questrate = worldConfig.quest_rate;
        int travelrate = worldConfig.travel_rate;
        int fishingrate = worldConfig.fishing_rate;

        int flag = worldConfig.flag;
        string event_message = worldConfig.event_message;
        string why_am_i_recommended = worldConfig.why_am_i_recommended;

        var world = new World(i,
                flag,
                event_message,
                exprate, droprate, bossdroprate, mesorate, questrate, travelrate, fishingrate);

        // world id从0开始 channel id从1开始
        Dictionary<int, string> channelInfo = new();
        long bootTime = getCurrentTime();
        for (int j = 1; j <= worldConfig.channels; j++)
        {
            int channelid = j;
            var channel = new WorldChannel(world, channelid, bootTime);

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
                channelInfoList.Insert(i, channelInfo);
            }
        }
        finally
        {
            wldLock.ExitWriteLock();
        }

        if (canDeploy)
        {
            world.setServerMessage(worldConfig.server_message);

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
        IWorld? world;

        wldLock.EnterReadLock();
        try
        {
            if (worldid >= worlds.Count)
            {
                return false;
            }
            world = worlds.ElementAtOrDefault(worldid);
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
                Dictionary<int, string> m = channelInfoList.get(worldid);
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
    {
        //lol don't!
        IWorld? w;
        int worldid;

        wldLock.EnterReadLock();
        try
        {
            worldid = worlds.Count - 1;
            if (worldid < 0)
            {
                return false;
            }

            w = worlds.ElementAtOrDefault(worldid);
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
                channelInfoList.remove(worldid);
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
            channelInfoList.Clear();
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
        foreach (var world in getWorlds())
        {
            foreach (var chr in world.getPlayerStorage().GetAllOnlinedPlayers())
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
        List<IClient> processDiseaseAnnounceClients;
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
            var c = processDiseaseAnnounceClients.remove(0);
            var player = c.getPlayer();
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
                var c = registeredDiseaseAnnouncePlayers.remove(0);
                processDiseaseAnnouncePlayers.Add(c);
            }
        }
        finally
        {
            Monitor.Exit(disLock);
        }
    }

    public void registerAnnouncePlayerDiseases(IClient c)
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

    public List<NameLevelPair> getWorldPlayerRanking(int worldid)
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

                    playerRanking.Insert(worldid, ranking.ElementAt(0).Value);
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
                    playerRanking[wranks.Key] = wranks.Value;
                }
            }
            else
            {
                playerRanking[0] = rankUpdates[0].Value;
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

    private static List<KeyValuePair<int, List<NameLevelPair>>> loadPlayerRankingFromDB(int worldid)
    {
        List<KeyValuePair<int, List<NameLevelPair>>> rankSystem = new();
        List<NameLevelPair> rankUpdate = new(0);

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
                        rankUpdate = new(50);
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

    public async Task Start()
    {
        log.Information("Cosmic v{Version} starting up.", ServerConstants.VERSION);
        Stopwatch totalSw = new Stopwatch();
        totalSw.Start();

        if (YamlConfig.config.server.SHUTDOWNHOOK)
        {
            AppDomain.CurrentDomain.ProcessExit += (obj, evt) => shutdown(false);
        }

        channelDependencies = registerChannelDependencies();

        _ = Task.Run(() =>
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            SkillFactory.loadAllSkills();
            sw.Stop();
            log.Debug("Skills loaded in {StarupCost}s", sw.Elapsed.TotalSeconds);
        });

        _ = Task.Run(() =>
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            CashItemFactory.loadAllCashItems();
            sw.Stop();
            log.Debug("CashItems loaded in {StarupCost}s", sw.Elapsed.TotalSeconds);
        });

        _ = Task.Run(() =>
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Quest.loadAllQuests();
            sw.Stop();
            log.Debug("Quest loaded in {StarupCost}s", sw.Elapsed.TotalSeconds);
        });

        _ = Task.Run(() =>
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            SkillbookInformationProvider.loadAllSkillbookInformation();
            sw.Stop();
            log.Debug("Skillbook loaded in {StarupCost}s", sw.Elapsed.TotalSeconds);
        });


        int worldCount = Math.Min(GameConstants.WORLD_NAMES.Length, YamlConfig.config.server.WORLDS);

        try
        {
            using var dbContext = new DBContext();
            setAllLoggedOut(dbContext);
            setAllMerchantsInactive(dbContext);
            cleanNxcodeCoupons(dbContext);
            loadCouponRates(dbContext);
            updateActiveCoupons(dbContext);
            NewYearCardRecord.startPendingNewYearCardRequests(dbContext);
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
        online = true;

        log.Information("Listening on port 8484");

        totalSw.Stop();
        log.Information("Cosmic is now online after {Startup} s.", totalSw.Elapsed.TotalSeconds);

        OpcodeConstants.generateOpcodeNames();
        CommandExecutor.getInstance();

        foreach (var ch in this.getAllChannels())
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
            var m = alliances.GetValueOrDefault(id);
            if (m == null)
            {
                m = AllianceManager.loadAlliance(id);
                if (m != null)
                {
                    alliances.Add(id, m);
                }
            }
            return m;
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
        return GuildManager.CreateGuild(name, leaderId);
    }

    public IGuild? getGuildByName(string name)
    {
        lock (guilds)
        {
            foreach (var mg in guilds.Values)
            {
                if (mg.getName().Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return mg;
                }
            }

            return null;
        }
    }

    public IGuild? getGuild(int id, IPlayer? mc = null)
    {
        lock (guilds)
        {
            var g = guilds.GetValueOrDefault(id);
            if (g != null)
            {
                return g;
            }

            g = GuildManager.FindGuildFromDB(id);
            if (g == null)
            {
                return null;
            }

            //if (mc != null)
            // g.setOnline(mc.getId(), true, mc.getClient().getChannel());

            guilds.AddOrUpdate(id, g);
            return g;
        }
    }

    public void setGuildMemberOnline(IPlayer mc, bool bOnline, int channel)
    {
        var g = getGuild(mc.getGuildId(), mc);
        if (g != null)
            // 当bOnline为true时 这里是否与 getGuild 中的setOnline发生了重复调用？
            g.setOnline(mc.getId(), bOnline, channel);
    }

    public bool setGuildAllianceId(int gId, int aId)
    {
        var guild = guilds.GetValueOrDefault(gId);
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

    public void leaveGuild(IPlayer mgc)
    {
        var g = guilds.GetValueOrDefault(mgc.GuildId);
        if (g != null)
        {
            g.leaveGuild(mgc);
        }
    }

    public void guildChat(int gid, string name, int cid, string msg)
    {
        var g = guilds.GetValueOrDefault(gid);
        if (g != null)
        {
            g.guildChat(name, cid, msg);
        }
    }

    public void changeRank(int gid, int cid, int newRank)
    {
        var g = guilds.GetValueOrDefault(gid);
        if (g != null)
        {
            g.changeRank(cid, newRank);
        }
    }

    public void expelMember(IPlayer initiator, string name, int cid)
    {
        var g = guilds.GetValueOrDefault(initiator.getGuildId());
        if (g != null)
        {
            g.expelMember(initiator, name, cid, channelDependencies.noteService);
        }
    }

    public void setGuildNotice(int gid, string notice)
    {
        var g = guilds.GetValueOrDefault(gid);
        if (g != null)
        {
            g.setGuildNotice(notice);
        }
    }

    public void memberLevelJobUpdate(IPlayer mgc)
    {
        var g = guilds.GetValueOrDefault(mgc.getGuildId());
        if (g != null)
        {
            g.memberLevelJobUpdate(mgc);
        }
    }

    public void changeRankTitle(int gid, string[] ranks)
    {
        var g = guilds.GetValueOrDefault(gid);
        if (g != null)
        {
            g.changeRankTitle(ranks);
        }
    }

    public void setGuildEmblem(int gid, short bg, byte bgcolor, short logo, byte logocolor)
    {
        var g = guilds.GetValueOrDefault(gid);
        if (g != null)
        {
            g.setGuildEmblem(bg, bgcolor, logo, logocolor);
        }
    }

    public void disbandGuild(int gid)
    {
        lock (guilds)
        {
            var g = guilds.GetValueOrDefault(gid);
            if (g == null)
                return;

            g.disbandGuild();
            guilds.Remove(gid);
        }
    }

    public bool increaseGuildCapacity(int gid)
    {
        var g = guilds.GetValueOrDefault(gid);
        if (g != null)
        {
            return g.increaseCapacity();
        }
        return false;
    }

    public void gainGP(int gid, int amount)
    {
        var g = guilds.GetValueOrDefault(gid);
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
        var g = guilds.GetValueOrDefault(gid);
        if (g != null)
        {
            g.broadcast(packet, exception);
        }
    }

    public PlayerBuffStorage getPlayerBuffStorage()
    {
        return buffStorage;
    }

    public void deleteGuildCharacter(IPlayer? mgc)
    {
        if (mgc == null)
            return;

        setGuildMemberOnline(mgc, false, -1);
        if (mgc.GuildRank > 1)
        {
            leaveGuild(mgc);
        }
        else
        {
            disbandGuild(mgc.GuildId);
        }
    }

    public void reloadGuildCharacters(int world)
    {
        var worlda = getWorld(world);
        foreach (var mc in worlda.getPlayerStorage().GetAllOnlinedPlayers())
        {
            if (mc.getGuildId() > 0)
            {
                setGuildMemberOnline(mc, true, worlda.getId());
                memberLevelJobUpdate(mc);
            }
        }
    }

    public void broadcastMessage(int world, Packet packet)
    {
        foreach (var ch in getChannelsFromWorld(world))
        {
            ch.broadcastPacket(packet);
        }
    }

    public void broadcastGMMessage(int world, Packet packet)
    {
        foreach (var ch in getChannelsFromWorld(world))
        {
            ch.broadcastGMPacket(packet);
        }
    }

    public bool isGmOnline(int world)
    {
        return getWorld(world).Players.GetAllOnlinedPlayers().Any(x => x.isGM());
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
            return AllPlayerStorage.GetOrAddCharacterById(chrid)?.World ?? -1;
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
            var accChars = AccountCharacterCache.GetValueOrDefault(accountid) ?? [];
            return accChars.Contains(chrid);
        }
        finally
        {
            lgnLock.ExitReadLock();
        }
    }

    public short getAccountCharacterCount(int accountid)
    {
        return (short)getAccountCharacterEntries(accountid).Count;
    }

    public short getAccountWorldCharacterCount(int accountid, int worldid)
    {
        return (short)getAccountCharacterEntries(accountid).Count(x => x.World == worldid);
    }

    private HashSet<IPlayer> getAccountCharacterEntries(int accountid, int loadLevel = 0)
    {
        lgnLock.EnterReadLock();
        try
        {
            if (AccountCharacterCache.ContainsKey(accountid))
                return AllPlayerStorage.GetPlayersByIds(AccountCharacterCache[accountid], loadLevel).ToHashSet();
            return [];
        }
        finally
        {
            lgnLock.ExitReadLock();
        }
    }

    public void createCharacterEntry(IPlayer chr)
    {
        int accountid = chr.getAccountID();

        lgnLock.EnterWriteLock();
        try
        {
            if (!AccountCharacterCache.ContainsKey(accountid))
                AccountCharacterCache.Add(accountid, new());

            AccountCharacterCache[accountid].Add(chr.Id);
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
            var accChars = AccountCharacterCache.GetValueOrDefault(accountid);
            if (accChars != null)
                accChars.Remove(chrid);
        }
        finally
        {
            lgnLock.ExitWriteLock();
        }
    }

    //public void transferWorldCharacterEntry(IPlayer chr, int toWorld)
    //{
    //    // used before setting the new worldid on the character object
    //    lgnLock.EnterWriteLock();
    //    try
    //    {
    //        int chrid = chr.getId(), accountid = chr.getAccountID();

    //        var wservTmp = this.getWorld(chr.World);
    //        if (wservTmp != null)
    //        {
    //            wservTmp.unregisterAccountCharacterView(accountid, chrid);
    //        }

    //        var wserv = this.getWorld(toWorld);
    //        if (wserv != null)
    //        {
    //            wserv.registerAccountCharacterView(chr.AccountId, chr);
    //        }
    //    }
    //    finally
    //    {
    //        lgnLock.ExitWriteLock();
    //    }
    //}

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

    /// <summary>
    /// world - players
    /// </summary>
    /// <param name="accountId"></param>
    /// <param name="visibleWorlds"></param>
    /// <returns></returns>
    public SortedDictionary<int, List<IPlayer>> LoadAccountCharList(int accountId, int visibleWorlds)
    {
        LoadAccountCharactersView(accountId, 0, visibleWorlds);

        var accountData = getAccountCharacterEntries(accountId, 1);
        SortedDictionary<int, List<IPlayer>> worldChrs = new();

        lgnLock.EnterReadLock();
        try
        {
            for (int worldId = 0; worldId < visibleWorlds; worldId++)
            {
                var chrs = accountData.Where(x => x.World == worldId).ToList();
                worldChrs.Add(worldId, chrs);
            }
        }
        finally
        {
            lgnLock.ExitReadLock();
        }

        return worldChrs;
    }


    public List<IPlayer> loadAllAccountsCharactersView()
    {
        using var dbContext = new DBContext();
        var idList = dbContext.Accounts.Select(x => x.Id).ToList();

        var list = new List<IPlayer>();
        idList.ForEach(accountId =>
        {
            LoadAccountCharactersView(accountId, 0, force: false);
            list.AddRange(getAccountCharacterEntries(accountId));
        });

        return list;
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
                bool success = CharacterManager.doNameChange(dbContext, x.Characterid, x.Old, x.New, x.Id);
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
                string? reason = CharacterManager.checkWorldTransferEligibility(dbContext, x.Characterid, x.From, x.To);
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
                var success = CharacterManager.doWorldTransfer(dbContext, x.Characterid, x.From, x.To, x.Id);
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

    public void loadAccountCharacters(IClient c)
    {
        int accId = c.getAccID();
        LoadAccountCharactersView(accId, 0);

        var accData = getAccountCharacterEntries(accId);
        int gmLevel = accData.Count == 0 ? 0 : accData.Max(x => x.gmLevel());
        c.setGMLevel(gmLevel);
    }

    private void LoadAccountCharactersView(int accId, int fromWorldid, int endWorldId = -1, bool force = true)
    {
        if (endWorldId == -1)
        {
            endWorldId = this.getWorlds().Count;
        }

        lgnLock.EnterWriteLock();
        try
        {
            if (!AccountCharacterCache.ContainsKey(accId))
            {
                AccountCharacterCache[accId] = new();
            }
            else
            {
                if (!force)
                    return;
            }

            var playerIdList = AccountManager.LoadAccountWorldPlayers(accId, Enumerable.Range(fromWorldid, endWorldId - fromWorldid));
            foreach (var cid in playerIdList)
            {
                AccountCharacterCache[accId].Add(cid);
            }
        }
        finally
        {
            lgnLock.ExitWriteLock();
        }
    }

    public void loadAccountStorages(IClient c)
    {
        int accountId = c.getAccID();
        var accWorlds = getAccountCharacterEntries(accountId).Select(x => x.World).ToHashSet();

        var worldList = this.getWorlds();
        foreach (int worldid in accWorlds)
        {
            if (worldid < worldList.Count)
            {
                var wserv = worldList.get(worldid);
                wserv.loadAccountStorage(accountId);
            }
        }
    }

    private static string getRemoteHost(IClient client)
    {
        return SessionCoordinator.getSessionRemoteHost(client);
    }

    public void setCharacteridInTransition(IClient client, int charId)
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

    public bool validateCharacteridInTransition(IClient client, int charId)
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

    public int? freeCharacteridInTransition(IClient client)
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

    public bool hasCharacteridInTransition(IClient client)
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

    public void registerLoginState(IClient c)
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

    public void unregisterLoginState(IClient c)
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
        List<IClient> toDisconnect = new();

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

            foreach (IClient c in toDisconnect)
            {
                inLoginState.Remove(c);
            }
        }
        finally
        {
            Monitor.Exit(srvLock);
        }

        foreach (IClient c in toDisconnect)
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
        TimerManager.getInstance().register(() => disconnectIdlesOnLoginState(), TimeSpan.FromMinutes(5));
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

        var allChannels = getAllChannels();

        foreach (var ch in allChannels)
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
            instance = new Lazy<Server>(new Server());
            await getInstance().Start();//DID I DO EVERYTHING?! D:
        }
    }
}
