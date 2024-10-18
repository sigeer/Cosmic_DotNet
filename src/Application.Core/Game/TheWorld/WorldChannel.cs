/*
This file is part of the OdinMS Maple Story NewServer
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


using Application.Core.Game.Maps;
using Application.Core.Game.Relation;
using Application.Core.Game.TheWorld;
using Application.Core.Managers;
using Application.Core.model;
using Application.Core.scripting.Event;
using constants.id;
using net.netty;
using net.packet;
using net.server.services;
using net.server.services.type;
using scripting.Event;
using server;
using server.events.gm;
using server.expeditions;
using server.maps;
using System.Text;
using tools;

namespace net.server.channel;

public class WorldChannel : IWorldChannel
{
    private ILogger log;
    private static int BASE_PORT = 7575;

    private int port;
    private string ip;
    private int world;
    private int channel;

    public ChannelPlayerStorage Players { get; }
    private ChannelServer channelServer;
    private string serverMessage;
    private MapManager mapManager;
    private EventScriptManager eventSM;
    private ServicesManager<ChannelServices> services;
    private Dictionary<int, HiredMerchant> hiredMerchants = new();
    private Dictionary<int, int> storedVars = new();
    private HashSet<int> playersAway = new();
    private Dictionary<ExpeditionType, Expedition> expeditions = new();
    private Dictionary<int, MiniDungeon> dungeons = new();
    private List<ExpeditionType> expedType = new();
    private HashSet<IMap> ownedMaps = new();
    private Event @event;
    private bool _finishedShutdown = false;
    private HashSet<int> usedMC = new();

    private int usedDojo = 0;
    private int[] dojoStage;
    private long[] dojoFinishTime;
    private ScheduledFuture?[] dojoTask;
    private Dictionary<int, int> dojoParty = new();

    private List<int> chapelReservationQueue = new();
    private List<int> cathedralReservationQueue = new();
    private ScheduledFuture? chapelReservationTask;
    private ScheduledFuture? cathedralReservationTask;

    private int? ongoingChapel = null;
    private bool? ongoingChapelType = null;
    private HashSet<int>? ongoingChapelGuests = null;
    private int? ongoingCathedral = null;
    private bool? ongoingCathedralType = null;
    private HashSet<int>? ongoingCathedralGuests = null;
    private long ongoingStartTime;

    private object lockObj = new object();
    private ReaderWriterLockSlim merchLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

    public IWorld WorldModel { get; set; }
    public WorldChannel(IWorld world, int channel, long startTime)
    {
        WorldModel = world;
        this.world = world.getId();
        this.channel = channel;
        Players = new ChannelPlayerStorage(this.world, channel);
        this.ongoingStartTime = startTime + 10000;  // rude approach to a world's last channel boot time, placeholder for the 1st wedding reservation ever
        this.mapManager = new MapManager(null, this.world, channel);
        this.port = BASE_PORT + (this.channel - 1) + (this.world * 100);
        this.ip = YamlConfig.config.server.HOST + ":" + port;
        log = LogFactory.GetLogger($"World_{this.world}/Channel_{channel}");
        try
        {
            this.channelServer = initServer(port, this.world, channel);
            expedType.AddRange(Arrays.asList(ExpeditionType.values<ExpeditionType>()));

            if (Server.getInstance().isOnline())
            {  // postpone event loading to improve boot time... thanks Riizade, daronhudson for noticing slow startup times
                eventSM = new EventScriptManager(this, getEvents());
                eventSM.init();
            }
            else
            {
                eventSM = new EventScriptManager(this, ["0_EXAMPLE"]);
            }


            dojoStage = new int[20];
            dojoFinishTime = new long[20];
            dojoTask = new ScheduledFuture[20];
            for (int i = 0; i < 20; i++)
            {
                dojoStage[i] = 0;
                dojoFinishTime[i] = 0;
                dojoTask[i] = null;
            }

            services = new ServicesManager<ChannelServices>(ChannelServices.OVERALL);

            log.Information("Channel {ChannelId}: Listening on port {Port}", getId(), port);
        }
        catch (Exception e)
        {
            log.Warning(e, "Error during channel initialization");
        }
    }

    private ChannelServer initServer(int port, int world, int channel)
    {
        ChannelServer channelServer = new ChannelServer(port, world, channel);
        channelServer.Start().Wait();
        return channelServer;
    }

    object loadEvetScriptLock = new object();
    public void reloadEventScriptManager()
    {
        lock (loadEvetScriptLock)
        {
            if (_finishedShutdown)
            {
                return;
            }

            eventSM.cancel();
            eventSM.dispose();
            eventSM = new EventScriptManager(this, getEvents());
        }

    }

    object shudownLock = new object();
    public void shutdown()
    {
        lock (shudownLock)
        {
            try
            {
                if (_finishedShutdown)
                {
                    return;
                }

                log.Information("Shutting down channel {ChannelId} in world {WorldId}", channel, world);

                closeAllMerchants();
                disconnectAwayPlayers();
                Players.disconnectAll();

                eventSM.dispose();

                mapManager.dispose();

                closeChannelSchedules();

                channelServer.Stop().Wait();

                _finishedShutdown = true;
                log.Information("Successfully shut down channel {ChannelId} in world {WorldId}", channel, world);
            }
            catch (Exception e)
            {
                log.Error(e, "Error while shutting down channel {ChannelId} in world {WorldId}", channel, world);
            }
        }
    }

    private void closeChannelServices()
    {
        services.shutdown();
    }

    private void closeChannelSchedules()
    {
        Monitor.Enter(lockObj);
        try
        {
            for (int i = 0; i < dojoTask.Length; i++)
            {
                if (dojoTask[i] != null)
                {
                    dojoTask[i]!.cancel(false);
                    dojoTask[i] = null;
                }
            }
        }
        finally
        {
            Monitor.Exit(lockObj);
        }

        closeChannelServices();
    }

    private void closeAllMerchants()
    {
        try
        {
            List<HiredMerchant> merchs;

            merchLock.EnterWriteLock();
            try
            {
                merchs = new(hiredMerchants.Values);
                hiredMerchants.Clear();
            }
            finally
            {
                merchLock.ExitWriteLock();
            }

            foreach (HiredMerchant merch in merchs)
            {
                merch.forceClose();
            }
        }
        catch (Exception e)
        {
            log.Error(e.ToString());
        }
    }

    public MapManager getMapFactory()
    {
        return mapManager;
    }

    public BaseService getServiceAccess(ChannelServices sv)
    {
        return services.getAccess(sv).getService();
    }

    public int getWorld()
    {
        return world;
    }

    public IWorld getWorldServer()
    {
        return Server.getInstance().getWorld(world)!;
    }

    public void addPlayer(IPlayer chr)
    {
        Players.AddPlayer(chr);
        chr.sendPacket(PacketCreator.serverMessage(serverMessage));
    }

    public string getServerMessage()
    {
        return serverMessage;
    }

    public ChannelPlayerStorage getPlayerStorage()
    {
        return Players;
    }

    public bool removePlayer(IPlayer chr)
    {
        return Players.RemovePlayer(chr.Id) != null;
    }

    public int getChannelCapacity()
    {
        return (int)(Math.Ceiling(((float)Players.getAllCharacters().Count / YamlConfig.config.server.CHANNEL_LOAD) * 800));
    }

    public void broadcastPacket(Packet packet)
    {
        foreach (var chr in Players.getAllCharacters())
        {
            chr.sendPacket(packet);
        }
    }

    public int getId()
    {
        return channel;
    }

    public string getIP()
    {
        return ip;
    }

    public Event getEvent()
    {
        return @event;
    }

    public void setEvent(Event evt)
    {
        this.@event = evt;
    }

    public EventScriptManager getEventSM()
    {
        return eventSM;
    }

    public void broadcastGMPacket(Packet packet)
    {
        foreach (var chr in Players.getAllCharacters())
        {
            if (chr.isGM())
            {
                chr.sendPacket(packet);
            }
        }
    }

    public List<IPlayer> getPartyMembers(ITeam party)
    {
        List<IPlayer> partym = new(8);
        foreach (var partychar in party.getMembers())
        {
            if (partychar.Channel == getId())
            {
                var chr = Players.getCharacterById(partychar.Id);
                if (chr != null)
                {
                    partym.Add(chr);
                }
            }
        }
        return partym;
    }

    public void insertPlayerAway(int chrId)
    {   // either they in CS or MTS
        playersAway.Add(chrId);
    }

    public void removePlayerAway(int chrId)
    {
        playersAway.Remove(chrId);
    }

    public bool canUninstall()
    {
        return Players.Count() == 0 && playersAway.Count == 0;
    }

    private void disconnectAwayPlayers()
    {
        var wserv = getWorldServer();
        foreach (int cid in playersAway)
        {
            var chr = wserv.Players.getCharacterById(cid);
            if (chr != null && chr.IsOnlined)
            {
                chr.getClient().forceDisconnect();
            }
        }
    }

    public Dictionary<int, HiredMerchant> getHiredMerchants()
    {
        merchLock.EnterReadLock();
        try
        {
            return new Dictionary<int, HiredMerchant>(hiredMerchants);
        }
        finally
        {
            merchLock.ExitReadLock();
        }
    }

    public void addHiredMerchant(int chrid, HiredMerchant hm)
    {
        merchLock.EnterWriteLock();
        try
        {
            hiredMerchants.AddOrUpdate(chrid, hm);
        }
        finally
        {
            merchLock.ExitWriteLock();
        }
    }

    public void removeHiredMerchant(int chrid)
    {
        merchLock.EnterWriteLock();
        try
        {
            hiredMerchants.Remove(chrid);
        }
        finally
        {
            merchLock.ExitWriteLock();
        }
    }

    public int[] multiBuddyFind(int charIdFrom, int[] characterIds)
    {
        List<int> ret = new(characterIds.Length);
        var playerStorage = Players;
        foreach (int characterId in characterIds)
        {
            var chr = playerStorage.getCharacterById(characterId);
            if (chr != null)
            {
                if (chr.BuddyList.containsVisible(charIdFrom))
                {
                    ret.Add(characterId);
                }
            }
        }
        return ret.ToArray();
    }

    public bool addExpedition(Expedition exped)
    {
        lock (expeditions)
        {
            if (expeditions.ContainsKey(exped.getType()))
            {
                return false;
            }

            expeditions.Add(exped.getType(), exped);
            exped.beginRegistration();  // thanks Conrad for noticing leader still receiving packets on failure-to-register cases
            return true;
        }
    }

    public void removeExpedition(Expedition exped)
    {
        lock (expeditions)
        {
            expeditions.Remove(exped.getType());
        }
    }

    public Expedition? getExpedition(ExpeditionType type)
    {
        return expeditions.GetValueOrDefault(type);
    }

    public List<Expedition> getExpeditions()
    {
        lock (expeditions)
        {
            return new(expeditions.Values);
        }
    }

    public bool isConnected(string name)
    {
        return Players.getCharacterByName(name) != null;
    }

    public bool isActive()
    {
        EventScriptManager esm = this.getEventSM();
        return esm != null && esm.isActive();
    }

    public bool finishedShutdown()
    {
        return _finishedShutdown;
    }

    public void setServerMessage(string message)
    {
        this.serverMessage = message;
        broadcastPacket(PacketCreator.serverMessage(message));
        getWorldServer().resetDisabledServerMessages();
    }

    private static string[] getEvents()
    {
        return Directory.GetFiles(Path.Combine("scripts", "event")).Select(x => Path.GetFileNameWithoutExtension(x)).ToArray();
    }

    public int getStoredVar(int key)
    {
        return storedVars.GetValueOrDefault(key);
    }

    public void setStoredVar(int key, int val)
    {
        this.storedVars.AddOrUpdate(key, val);
    }

    public int lookupPartyDojo(ITeam? party)
    {
        if (party == null)
        {
            return -1;
        }

        return dojoParty.get(party.GetHashCode()) ?? -1;
    }

    public int ingressDojo(bool isPartyDojo, int fromStage)
    {
        return ingressDojo(isPartyDojo, null, fromStage);
    }

    public int ingressDojo(bool isPartyDojo, ITeam? party, int fromStage)
    {
        Monitor.Enter(lockObj);
        try
        {
            int dojoList = this.usedDojo;
            int range, slot = 0;

            if (!isPartyDojo)
            {
                dojoList = dojoList >> 5;
                range = 15;
            }
            else
            {
                range = 5;
            }

            while ((dojoList & 1) != 0)
            {
                dojoList = (dojoList >> 1);
                slot++;
            }

            if (slot < range)
            {
                int slotMapid = (isPartyDojo ? MapId.DOJO_PARTY_BASE : MapId.DOJO_SOLO_BASE) + (100 * (fromStage + 1)) + slot;
                int dojoSlot = getDojoSlot(slotMapid);

                if (party != null)
                {
                    if (dojoParty.ContainsKey(party.GetHashCode()))
                    {
                        return -2;
                    }
                    dojoParty.Add(party.GetHashCode(), dojoSlot);
                }

                this.usedDojo |= (1 << dojoSlot);

                this.resetDojo(slotMapid);
                this.startDojoSchedule(slotMapid);
                return slot;
            }
            else
            {
                return -1;
            }
        }
        finally
        {
            Monitor.Exit(lockObj);
        }
    }

    private void freeDojoSlot(int slot, ITeam? party)
    {
        int mask = 0b11111111111111111111;
        mask ^= (1 << slot);

        Monitor.Enter(lockObj);
        try
        {
            usedDojo &= mask;
        }
        finally
        {
            Monitor.Exit(lockObj);
        }

        if (party != null)
        {
            if (dojoParty.remove(party.GetHashCode()) != null)
            {
                return;
            }
        }

        if (dojoParty.ContainsValue(slot))
        {    // strange case, no party there!
            HashSet<KeyValuePair<int, int>> es = new(dojoParty);

            foreach (var e in es)
            {
                if (e.Value == slot)
                {
                    dojoParty.Remove(e.Key);
                    break;
                }
            }
        }
    }

    private static int getDojoSlot(int dojoMapId)
    {
        return (dojoMapId % 100) + ((dojoMapId / 10000 == 92502) ? 5 : 0);
    }

    public void resetDojoMap(int fromMapId)
    {
        for (int i = 0; i < (((fromMapId / 100) % 100 <= 36) ? 5 : 2); i++)
        {
            this.getMapFactory().getMap(fromMapId + (100 * i)).resetMapObjects();
        }
    }

    public void resetDojo(int dojoMapId)
    {
        resetDojo(dojoMapId, -1);
    }

    private void resetDojo(int dojoMapId, int thisStg)
    {
        int slot = getDojoSlot(dojoMapId);
        this.dojoStage[slot] = thisStg;
    }

    public void freeDojoSectionIfEmpty(int dojoMapId)
    {
        int slot = getDojoSlot(dojoMapId);
        int delta = (dojoMapId) % 100;
        int stage = (dojoMapId / 100) % 100;
        int dojoBaseMap = (dojoMapId >= MapId.DOJO_PARTY_BASE) ? MapId.DOJO_PARTY_BASE : MapId.DOJO_SOLO_BASE;

        for (int i = 0; i < 5; i++)
        { //only 32 stages, but 38 maps
            if (stage + i > 38)
            {
                break;
            }
            var dojoMap = getMapFactory().getMap(dojoBaseMap + (100 * (stage + i)) + delta);
            if (dojoMap.getAllPlayers().Count > 0)
            {
                return;
            }
        }

        freeDojoSlot(slot, null);
    }

    private void startDojoSchedule(int dojoMapId)
    {
        int slot = getDojoSlot(dojoMapId);
        int stage = (dojoMapId / 100) % 100;
        if (stage <= dojoStage[slot])
        {
            return;
        }

        long clockTime = (stage > 36 ? 15 : (stage / 6) + 5) * 60000;

        Monitor.Enter(lockObj);
        try
        {
            if (this.dojoTask[slot] != null)
            {
                this.dojoTask[slot]!.cancel(false);
            }
            this.dojoTask[slot] = TimerManager.getInstance().schedule(() =>
            {
                int delta = (dojoMapId) % 100;
                int dojoBaseMap = (slot < 5) ? MapId.DOJO_PARTY_BASE : MapId.DOJO_SOLO_BASE;
                ITeam? party = null;

                for (int i = 0; i < 5; i++)
                { //only 32 stages, but 38 maps
                    if (stage + i > 38)
                    {
                        break;
                    }

                    var dojoExit = getMapFactory().getMap(MapId.DOJO_EXIT);
                    foreach (var chr in getMapFactory().getMap(dojoBaseMap + (100 * (stage + i)) + delta).getAllPlayers())
                    {
                        if (MapId.isDojo(chr.getMap().getId()))
                        {
                            chr.changeMap(dojoExit);
                        }
                        party = chr.getParty();
                    }
                }

                freeDojoSlot(slot, party);
            }, clockTime + 3000);   // let the TIMES UP display for 3 seconds, then warp
        }
        finally
        {
            Monitor.Exit(lockObj);
        }

        dojoFinishTime[slot] = Server.getInstance().getCurrentTime() + clockTime;
    }

    public void dismissDojoSchedule(int dojoMapId, ITeam party)
    {
        int slot = getDojoSlot(dojoMapId);
        int stage = (dojoMapId / 100) % 100;
        if (stage <= dojoStage[slot])
        {
            return;
        }

        Monitor.Enter(lockObj);
        try
        {
            if (this.dojoTask[slot] != null)
            {
                this.dojoTask[slot]!.cancel(false);
                this.dojoTask[slot] = null;
            }
        }
        finally
        {
            Monitor.Exit(lockObj);
        }

        freeDojoSlot(slot, party);
    }

    public bool setDojoProgress(int dojoMapId)
    {
        int slot = getDojoSlot(dojoMapId);
        int dojoStg = (dojoMapId / 100) % 100;

        if (this.dojoStage[slot] < dojoStg)
        {
            this.dojoStage[slot] = dojoStg;
            return true;
        }
        else
        {
            return false;
        }
    }

    public long getDojoFinishTime(int dojoMapId)
    {
        return dojoFinishTime[getDojoSlot(dojoMapId)];
    }

    public bool addMiniDungeon(int dungeonid)
    {
        Monitor.Enter(lockObj);
        try
        {
            if (dungeons.ContainsKey(dungeonid))
            {
                return false;
            }

            MiniDungeonInfo mmdi = MiniDungeonInfo.getDungeon(dungeonid);
            MiniDungeon mmd = new MiniDungeon(mmdi.getBase(), this.getMapFactory().getMap(mmdi.getDungeonId()).getTimeLimit());   // thanks Conrad for noticing hardcoded time limit for minidungeons

            dungeons.Add(dungeonid, mmd);
            return true;
        }
        finally
        {
            Monitor.Exit(lockObj);
        }
    }

    public MiniDungeon? getMiniDungeon(int dungeonid)
    {
        Monitor.Enter(lockObj);
        try
        {
            return dungeons.GetValueOrDefault(dungeonid);
        }
        finally
        {
            Monitor.Exit(lockObj);
        }
    }

    public void removeMiniDungeon(int dungeonid)
    {
        Monitor.Enter(lockObj);
        try
        {
            dungeons.Remove(dungeonid);
        }
        finally
        {
            Monitor.Exit(lockObj);
        }
    }

    public KeyValuePair<bool, KeyValuePair<int, HashSet<int>>>? getNextWeddingReservation(bool cathedral)
    {
        int? ret;

        Monitor.Enter(lockObj);
        try
        {
            List<int> weddingReservationQueue = (cathedral ? cathedralReservationQueue : chapelReservationQueue);
            if (weddingReservationQueue.Count == 0)
            {
                return null;
            }

            ret = weddingReservationQueue.remove(0);
            if (ret == null)
            {
                return null;
            }
        }
        finally
        {
            Monitor.Exit(lockObj);
        }

        var wserv = getWorldServer();

        var coupleId = wserv.getMarriageQueuedCouple(ret.Value)!;
        var typeGuests = wserv.removeMarriageQueued(ret.Value);

        CoupleNamePair couple = new(CharacterManager.getNameById(coupleId.HusbandId), CharacterManager.getNameById(coupleId.WifeId));
        wserv.dropMessage(6, couple.CharacterName1 + " and " + couple.CharacterName2 + "'s wedding is going to be started at " + (cathedral ? "Cathedral" : "Chapel") + " on Channel " + channel + ".");

        return new(typeGuests.Key, new(ret.Value, typeGuests.Value));
    }

    public bool isWeddingReserved(int weddingId)
    {
        var wserv = getWorldServer();

        Monitor.Enter(lockObj);
        try
        {
            return wserv.isMarriageQueued(weddingId) || weddingId.Equals(ongoingCathedral) || weddingId.Equals(ongoingChapel);
        }
        finally
        {
            Monitor.Exit(lockObj);
        }
    }

    public int getWeddingReservationStatus(int? weddingId, bool cathedral)
    {
        if (weddingId == null)
        {
            return -1;
        }

        Monitor.Enter(lockObj);
        try
        {
            if (cathedral)
            {
                if (weddingId.Equals(ongoingCathedral))
                {
                    return 0;
                }

                for (int i = 0; i < cathedralReservationQueue.Count; i++)
                {
                    if (weddingId == cathedralReservationQueue[i])
                    {
                        return i + 1;
                    }
                }
            }
            else
            {
                if (weddingId.Equals(ongoingChapel))
                {
                    return 0;
                }

                for (int i = 0; i < chapelReservationQueue.Count; i++)
                {
                    if (weddingId == chapelReservationQueue[i])
                    {
                        return i + 1;
                    }
                }
            }

            return -1;
        }
        finally
        {
            Monitor.Exit(lockObj);
        }
    }

    public int pushWeddingReservation(int? weddingId, bool cathedral, bool premium, int groomId, int brideId)
    {
        if (weddingId == null || isWeddingReserved(weddingId.Value))
        {
            return -1;
        }

        var wserv = getWorldServer();
        wserv.putMarriageQueued(weddingId.Value, cathedral, premium, groomId, brideId);

        Monitor.Enter(lockObj);
        try
        {
            List<int> weddingReservationQueue = (cathedral ? cathedralReservationQueue : chapelReservationQueue);

            int delay = YamlConfig.config.server.WEDDING_RESERVATION_DELAY - 1 - weddingReservationQueue.Count;
            for (int i = 0; i < delay; i++)
            {
                weddingReservationQueue.Add(0);  // push empty slots to fill the waiting time
            }

            weddingReservationQueue.Add(weddingId.Value);
            return weddingReservationQueue.Count;
        }
        finally
        {
            Monitor.Exit(lockObj);
        }
    }

    public bool isOngoingWeddingGuest(bool cathedral, int playerId)
    {
        Monitor.Enter(lockObj);
        try
        {
            if (cathedral)
            {
                return ongoingCathedralGuests != null && ongoingCathedralGuests.Contains(playerId);
            }
            else
            {
                return ongoingChapelGuests != null && ongoingChapelGuests.Contains(playerId);
            }
        }
        finally
        {
            Monitor.Exit(lockObj);
        }
    }

    public int getOngoingWedding(bool cathedral)
    {
        Monitor.Enter(lockObj);
        try
        {
            return (cathedral ? ongoingCathedral : ongoingChapel) ?? 0;
        }
        finally
        {
            Monitor.Exit(lockObj);
        }
    }

    //public bool getOngoingWeddingType(bool cathedral)
    //{
    //    Monitor.Enter(lockObj);
    //    try
    //    {
    //        return (cathedral ? ongoingCathedralType : ongoingChapelType) ?? false;
    //    }
    //    finally
    //    {
    //        Monitor.Exit(lockObj);
    //    }
    //}

    public void closeOngoingWedding(bool cathedral)
    {
        Monitor.Enter(lockObj);
        try
        {
            if (cathedral)
            {
                ongoingCathedral = null;
                ongoingCathedralType = null;
                ongoingCathedralGuests = null;
            }
            else
            {
                ongoingChapel = null;
                ongoingChapelType = null;
                ongoingChapelGuests = null;
            }
        }
        finally
        {
            Monitor.Exit(lockObj);
        }
    }

    public void setOngoingWedding(bool cathedral, bool? premium, int? weddingId, HashSet<int>? guests)
    {
        Monitor.Enter(lockObj);
        try
        {
            if (cathedral)
            {
                ongoingCathedral = weddingId;
                ongoingCathedralType = premium;
                ongoingCathedralGuests = guests;
            }
            else
            {
                ongoingChapel = weddingId;
                ongoingChapelType = premium;
                ongoingChapelGuests = guests;
            }
        }
        finally
        {
            Monitor.Exit(lockObj);
        }

        ongoingStartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (weddingId != null)
        {
            ScheduledFuture? weddingTask = TimerManager.getInstance().schedule(() => closeOngoingWedding(cathedral), TimeSpan.FromMinutes(YamlConfig.config.server.WEDDING_RESERVATION_TIMEOUT));

            if (cathedral)
            {
                cathedralReservationTask = weddingTask;
            }
            else
            {
                chapelReservationTask = weddingTask;
            }
        }
    }

    object weddingLock = new object();
    public bool acceptOngoingWedding(bool cathedral)
    {
        lock (weddingLock)
        {
            // couple succeeded to show up and started the ceremony
            if (cathedral)
            {
                if (cathedralReservationTask == null)
                {
                    return false;
                }

                cathedralReservationTask.cancel(false);
                cathedralReservationTask = null;
            }
            else
            {
                if (chapelReservationTask == null)
                {
                    return false;
                }

                chapelReservationTask.cancel(false);
                chapelReservationTask = null;
            }

            return true;
        }
    }

    private static string? getTimeLeft(long futureTime)
    {
        StringBuilder str = new StringBuilder();
        long leftTime = futureTime - DateTimeOffset.Now.ToUnixTimeMilliseconds();

        if (leftTime < 0)
        {
            return null;
        }

        byte mode = 0;
        if ((leftTime / 60 * 1000) > 0)
        {
            mode++;     //counts minutes

            if (leftTime / (3600 * 1000) > 0)
            {
                mode++;     //counts hours
            }
        }

        switch (mode)
        {
            case 2:
                int hours = (int)((leftTime / (3600 * 1000)));
                str.Append(hours + " hours, ");
                break;
            case 1:
                int minutes = (int)((leftTime / (60 * 1000)) % 60);
                str.Append(minutes + " minutes, ");
                break;
            default:
                int seconds = (int)(leftTime / 1000) % 60;
                str.Append(seconds + " seconds");
                break;
        }

        return str.ToString();
    }

    public long getWeddingTicketExpireTime(int resSlot)
    {
        return ongoingStartTime + getRelativeWeddingTicketExpireTime(resSlot);
    }

    public static long getRelativeWeddingTicketExpireTime(int resSlot)
    {
        return (long)resSlot * YamlConfig.config.server.WEDDING_RESERVATION_INTERVAL * 60 * 1000;
    }

    public string? getWeddingReservationTimeLeft(int? weddingId)
    {
        if (weddingId == null)
        {
            return null;
        }

        Monitor.Enter(lockObj);
        try
        {
            bool cathedral = true;

            int resStatus;
            resStatus = getWeddingReservationStatus(weddingId, true);
            if (resStatus < 0)
            {
                cathedral = false;
                resStatus = getWeddingReservationStatus(weddingId, false);

                if (resStatus < 0)
                {
                    return null;
                }
            }

            string venue = (cathedral ? "Cathedral" : "Chapel");
            if (resStatus == 0)
            {
                return venue + " - RIGHT NOW";
            }

            return venue + " - " + getTimeLeft(ongoingStartTime + (long)resStatus * YamlConfig.config.server.WEDDING_RESERVATION_INTERVAL * 60 * 1000) + " from now";
        }
        finally
        {
            Monitor.Exit(lockObj);
        }
    }

    public CoupleIdPair? getWeddingCoupleForGuest(int guestId, bool cathedral)
    {
        Monitor.Enter(lockObj);
        try
        {
            return (isOngoingWeddingGuest(cathedral, guestId)) ? getWorldServer().getRelationshipCouple(getOngoingWedding(cathedral)) : null;
        }
        finally
        {
            Monitor.Exit(lockObj);
        }
    }

    public void dropMessage(int type, string message)
    {
        foreach (var player in getPlayerStorage().getAllCharacters())
        {
            player.dropMessage(type, message);
        }
    }

    public void registerOwnedMap(IMap map)
    {
        ownedMaps.Add(map);
    }

    public void unregisterOwnedMap(IMap map)
    {
        ownedMaps.Remove(map);
    }

    public void runCheckOwnedMapsSchedule()
    {
        if (ownedMaps.Count > 0)
        {
            List<IMap> ownedMapsList;

            lock (ownedMaps)
            {
                ownedMapsList = new(ownedMaps);
            }

            foreach (var map in ownedMapsList)
            {
                map.checkMapOwnerActivity();
            }
        }
    }

    private static int getMonsterCarnivalRoom(bool cpq1, int field)
    {
        return (cpq1 ? 0 : 100) + field;
    }

    public void initMonsterCarnival(bool cpq1, int field)
    {
        usedMC.Add(getMonsterCarnivalRoom(cpq1, field));
    }

    public void finishMonsterCarnival(bool cpq1, int field)
    {
        usedMC.Remove(getMonsterCarnivalRoom(cpq1, field));
    }

    public bool canInitMonsterCarnival(bool cpq1, int field)
    {
        return !usedMC.Contains(getMonsterCarnivalRoom(cpq1, field));
    }

    public void debugMarriageStatus()
    {
        log.Debug(" ----- WORLD DATA -----");
        getWorldServer().debugMarriageStatus();

        log.Debug(" ----- CH. {ChannelId} -----", channel);
        log.Debug(" ----- CATHEDRAL -----");
        log.Debug("Current Queue: {0}", cathedralReservationQueue);
        log.Debug("Cancel Task?: {0}", cathedralReservationTask != null);
        log.Debug("Ongoing wid: {0}", ongoingCathedral);
        log.Debug("Ongoing wid: {0}, isPremium: {1}", ongoingCathedral, ongoingCathedralType);
        log.Debug("Guest list: {0}", ongoingCathedralGuests);
        log.Debug(" ----- CHAPEL -----");
        log.Debug("Current Queue: {0}", chapelReservationQueue);
        log.Debug("Cancel Task?: {0}", chapelReservationTask != null);
        log.Debug("Ongoing wid: {0}", ongoingChapel);
        log.Debug("Ongoing wid: {0}, isPremium: {1}", ongoingChapel, ongoingChapelType);
        log.Debug("Guest list: {0}", ongoingChapelGuests);
        log.Debug("Starttime: {0}", ongoingStartTime);
    }
}
