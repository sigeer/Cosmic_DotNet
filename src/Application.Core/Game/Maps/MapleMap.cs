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


using Application.Core.Game.Life;
using Application.Core.Game.Life.Monsters;
using Application.Core.Game.Maps.AnimatedObjects;
using Application.Core.Game.Skills;
using Application.Core.Game.TheWorld;
using Application.Core.scripting.Event;
using Application.Utility;
using client;
using client.autoban;
using client.inventory;
using client.status;
using constants.game;
using constants.id;
using constants.inventory;
using net.packet;
using net.server;
using net.server.coordinator.world;
using net.server.services.task.channel;
using net.server.services.type;
using scripting.Event;
using scripting.map;
using server;
using server.events.gm;
using server.life;
using server.maps;
using server.partyquest;
using System.Collections.ObjectModel;
using System.Text;
using tools;
using static server.partyquest.CarnivalFactory;


namespace Application.Core.Game.Maps;

public class MapleMap : IMap
{
    private ILogger log;
    private static List<MapObjectType> rangedMapobjectTypes = Arrays.asList(MapObjectType.SHOP, MapObjectType.ITEM, MapObjectType.NPC, MapObjectType.MONSTER, MapObjectType.DOOR, MapObjectType.SUMMON, MapObjectType.REACTOR);
    private static Dictionary<int, KeyValuePair<int, int>?> dropBoundsCache = new(100);

    private Dictionary<int, IMapObject> mapobjects = new();
    private HashSet<int> selfDestructives = new();
    private List<SpawnPoint> monsterSpawn = new();
    private List<SpawnPoint> allMonsterSpawn = new();
    private AtomicInteger spawnedMonstersOnMap = new AtomicInteger(0);
    public AtomicInteger droppedItemCount { get; set; } = new AtomicInteger(0);
    private HashSet<IPlayer> characters = new();
    private Dictionary<int, HashSet<int>> mapParty = new();
    private Dictionary<int, Portal> portals = new();
    private Dictionary<int, int> backgroundTypes = new();
    private Dictionary<string, int> environment = new();
    private Dictionary<MapItem, long> droppedItems = new();
    private List<WeakReference<IMapObject>> registeredDrops = new();
    private Dictionary<MobLootEntry, long> mobLootEntries = new(20);
    private List<Action> statUpdateRunnables = new(50);
    private List<Rectangle> areas = new();
    private FootholdTree? footholds = null;
    private KeyValuePair<int, int> xLimits;  // caches the min and max x's with available footholds
    private Rectangle mapArea = new Rectangle();
    private int mapid;
    private AtomicInteger runningOid = new AtomicInteger(1000000001);
    private int returnMapId;
    private int channel;
    private int world;
    private int seats;
    private byte monsterRate;
    private bool clock;
    private bool boat;
    private bool docked = false;
    private EventInstanceManager? @event = null;
    private string mapName;
    private string streetName;
    private MapEffect? mapEffect = null;
    private bool everlast = false;
    private int forcedReturnMap = MapId.NONE;
    private int timeLimit;
    private long mapTimer;
    private int decHP = 0;
    private float recovery = 1.0f;
    private int protectItem = 0;
    private bool town;
    private OxQuiz? ox;
    private bool _isOxQuiz = false;
    private bool dropsOn = true;
    private string onFirstUserEnter;
    private string onUserEnter;
    private int fieldType;
    private int fieldLimit = 0;
    private int mobCapacity = -1;
    private MonsterAggroCoordinator? aggroMonitor = null;   // aggroMonitor activity in sync with itemMonitor
    private ScheduledFuture? itemMonitor = null;
    private ScheduledFuture? expireItemsTask = null;
    private ScheduledFuture? mobSpawnLootTask = null;
    private ScheduledFuture? characterStatUpdateTask = null;
    private short itemMonitorTimeout;
    private KeyValuePair<int, string>? timeMob = null;
    private short mobInterval = 5000;
    private bool _allowSummons = true; // All maps should have this true at the beginning
    private IPlayer? mapOwner = null;
    private long mapOwnerLastActivityTime = long.MaxValue;

    // events
    private bool eventstarted = false, _isMuted = false;
    private Snowball? snowball0 = null;
    private Snowball? snowball1 = null;
    private Coconut? coconut;

    //CPQ
    private int maxMobs;
    private int maxReactors;
    private int deathCP;
    private int timeDefault;
    private int timeExpand;

    //locks
    ReaderWriterLockSlim chrLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    ReaderWriterLockSlim objectLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

    private object lootLock = new object();

    // due to the nature of loadMapFromWz (synchronized), sole function that calls 'generateMapDropRangeCache', this lock remains optional.
    private static object bndLock = new object();

    public MapleMap(int mapid, int world, int channel, int returnMapId, float monsterRate)
    {
        this.mapid = mapid;
        this.channel = channel;
        this.world = world;
        this.returnMapId = returnMapId;
        this.monsterRate = (byte)Math.Ceiling(monsterRate);
        if (this.monsterRate == 0)
        {
            this.monsterRate = 1;
        }
        aggroMonitor = new MonsterAggroCoordinator();
        onFirstUserEnter = mapid.ToString();
        onUserEnter = mapid.ToString();
        mapName = "";
        streetName = "";

        var range = new RangeNumberGenerator(mapid, 100000000);
        log = LogFactory.GetLogger($"Map/{range}");
    }

    public void setEventInstance(EventInstanceManager? eim)
    {
        @event = eim;
    }

    public EventInstanceManager? getEventInstance()
    {
        return @event;
    }

    public Rectangle getMapArea()
    {
        return mapArea;
    }

    public int getWorld()
    {
        return world;
    }

    public void broadcastPacket(IPlayer source, Packet packet)
    {
        broadcastPacket(packet, chr => chr != source);
    }

    public void broadcastGMPacket(IPlayer source, Packet packet)
    {
        broadcastPacket(packet, chr => chr != source && chr.gmLevel() >= source.gmLevel());
    }

    private void broadcastPacket(Packet packet, Func<IPlayer, bool> chrFilter)
    {
        chrLock.EnterReadLock();
        try
        {
            characters.Where(chrFilter).ToList()
                    .ForEach(chr => chr.sendPacket(packet));
        }
        finally
        {
            chrLock.ExitReadLock();
        }
    }

    public void toggleDrops()
    {
        this.dropsOn = !dropsOn;
    }

    private static double getRangedDistance()
    {
        return YamlConfig.config.server.USE_MAXRANGE ? double.PositiveInfinity : 722500;
    }

    public List<IMapObject> getMapObjectsInRect(Rectangle box, List<MapObjectType> types)
    {
        objectLock.EnterReadLock();
        List<IMapObject> ret = new();
        try
        {
            foreach (IMapObject l in mapobjects.Values)
            {
                if (types.Contains(l.getType()))
                {
                    if (box.Contains(l.getPosition()))
                    {
                        ret.Add(l);
                    }
                }
            }
        }
        finally
        {
            objectLock.ExitReadLock();
        }
        return ret;
    }

    public int getId()
    {
        return mapid;
    }

    public IWorldChannel getChannelServer()
    {
        return getWorldServer().getChannel(channel);
    }

    public IWorld getWorldServer()
    {
        return Server.getInstance().getWorld(world)!;
    }

    public IMap getReturnMap()
    {
        if (returnMapId == MapId.NONE)
        {
            return this;
        }
        return getChannelServer().getMapFactory().getMap(returnMapId);
    }

    public int getReturnMapId()
    {
        return returnMapId;
    }

    public IMap getForcedReturnMap()
    {
        return getChannelServer().getMapFactory().getMap(forcedReturnMap);
    }

    public int getForcedReturnId()
    {
        return forcedReturnMap;
    }

    public void setForcedReturnMap(int map)
    {
        this.forcedReturnMap = map;
    }

    public int getTimeLimit()
    {
        return timeLimit;
    }

    public void setTimeLimit(int timeLimit)
    {
        this.timeLimit = timeLimit;
    }

    public int getTimeLeft()
    {
        return (int)((mapTimer - DateTimeOffset.Now.ToUnixTimeMilliseconds()) / 1000);
    }

    public void setReactorState()
    {
        foreach (IMapObject o in getMapObjects())
        {
            if (o.getType() == MapObjectType.REACTOR)
            {
                if (((Reactor)o).getState() < 1)
                {
                    Reactor mr = (Reactor)o;
                    mr.lockReactor();
                    try
                    {
                        mr.resetReactorActions(1);
                        broadcastMessage(PacketCreator.triggerReactor((Reactor)o, 1));
                    }
                    finally
                    {
                        mr.unlockReactor();
                    }
                }
            }
        }
    }

    public void limitReactor(int rid, int num)
    {
        List<Reactor> toDestroy = new();
        Dictionary<int, int> contained = new();

        foreach (IMapObject obj in getReactors())
        {
            Reactor mr = (Reactor)obj;
            if (contained.ContainsKey(mr.getId()))
            {
                var containedData = contained.get(mr.getId());
                if (containedData != null)
                {
                    if (containedData >= num)
                    {
                        toDestroy.Add(mr);
                    }
                    else
                    {
                        contained.AddOrUpdate(mr.getId(), containedData.Value + 1);
                    }
                }

            }
            else
            {
                contained.AddOrUpdate(mr.getId(), 1);
            }
        }

        foreach (Reactor mr in toDestroy)
        {
            destroyReactor(mr.getObjectId());
        }
    }

    public bool isAllReactorState(int reactorId, int state)
    {
        foreach (var mo in getReactors())
        {
            Reactor r = (Reactor)mo;

            if (r.getId() == reactorId && r.getState() != state)
            {
                return false;
            }
        }
        return true;
    }

    public int getCurrentPartyId()
    {
        foreach (IPlayer chr in this.getCharacters())
        {
            if (chr.getPartyId() != -1)
            {
                return chr.getPartyId();
            }
        }
        return -1;
    }

    public void addPlayerNPCMapObject(PlayerNPC pnpcobject)
    {
        objectLock.EnterWriteLock();
        try
        {
            this.mapobjects.AddOrUpdate(pnpcobject.getObjectId(), pnpcobject);
        }
        finally
        {
            objectLock.ExitWriteLock();
        }
    }

    public void addMapObject(IMapObject mapobject)
    {
        int curOID = getUsableOID();

        objectLock.EnterWriteLock();
        try
        {
            mapobject.setObjectId(curOID);
            this.mapobjects.AddOrUpdate(curOID, mapobject);
        }
        finally
        {
            objectLock.ExitWriteLock();
        }
    }

    public void addSelfDestructive(Monster mob)
    {
        if (mob.getStats().selfDestruction() != null)
        {
            this.selfDestructives.Add(mob.getObjectId());
        }
    }

    public bool removeSelfDestructive(int mapobjectid)
    {
        return this.selfDestructives.Remove(mapobjectid);
    }

    private void spawnAndAddRangedMapObject(IMapObject mapobject, Action<IClient>? packetbakery)
    {
        spawnAndAddRangedMapObject(mapobject, packetbakery, null);
    }

    private void spawnAndAddRangedMapObject(IMapObject mapobject, Action<IClient>? packetbakery, Func<IPlayer, bool>? condition)
    {
        List<IPlayer> inRangeCharacters = new();
        int curOID = getUsableOID();

        chrLock.EnterReadLock();
        objectLock.EnterWriteLock();
        try
        {
            mapobject.setObjectId(curOID);
            this.mapobjects.AddOrUpdate(curOID, mapobject);
            foreach (IPlayer chr in characters)
            {
                if (condition == null || condition.Invoke(chr))
                {
                    if (chr.getPosition().distanceSq(mapobject.getPosition()) <= getRangedDistance())
                    {
                        inRangeCharacters.Add(chr);
                        chr.addVisibleMapObject(mapobject);
                    }
                }
            }
        }
        finally
        {
            objectLock.ExitWriteLock();
            chrLock.ExitReadLock();
        }

        foreach (IPlayer chr in inRangeCharacters)
        {
            packetbakery?.Invoke(chr.getClient());
        }
    }

    //private void spawnRangedMapObject(IMapObject mapobject, DelayedPacketCreation packetbakery, SpawnCondition condition)
    //{
    //    List<IPlayer> inRangeCharacters = new();

    //    chrLock.EnterReadLock();
    //    try
    //    {
    //        int curOID = getUsableOID();
    //        mapobject.setObjectId(curOID);
    //        foreach (IPlayer chr in characters)
    //        {
    //            if (condition == null || (condition.canSpawn?.Invoke(chr) ?? false))
    //            {
    //                if (chr.getPosition().distanceSq(mapobject.getPosition()) <= getRangedDistance())
    //                {
    //                    inRangeCharacters.Add(chr);
    //                    chr.addVisibleMapObject(mapobject);
    //                }
    //            }
    //        }
    //    }
    //    finally
    //    {
    //        chrLock.ExitReadLock();
    //    }

    //    foreach (IPlayer chr in inRangeCharacters)
    //    {
    //        packetbakery.sendPackets?.Invoke(chr.getClient());
    //    }
    //}

    private int getUsableOID()
    {
        objectLock.EnterReadLock();
        try
        {
            int curOid;

            // clashes with playernpc on curOid >= 2147000000, developernpc uses >= 2147483000
            do
            {
                if ((curOid = runningOid.incrementAndGet()) >= 2147000000)
                {
                    runningOid.set(curOid = 1000000001);
                }
            } while (mapobjects.ContainsKey(curOid));

            return curOid;
        }
        finally
        {
            objectLock.ExitReadLock();
        }
    }

    public void removeMapObject(int num)
    {
        objectLock.EnterWriteLock();
        try
        {
            this.mapobjects.Remove(num);
        }
        finally
        {
            objectLock.ExitWriteLock();
        }
    }

    public void removeMapObject(IMapObject obj)
    {
        removeMapObject(obj.getObjectId());
    }

    private Point? calcPointBelow(Point initial)
    {
        Foothold? fh = footholds?.findBelow(initial);
        if (fh == null)
        {
            return null;
        }
        int dropY = fh.getY1();
        if (!fh.isWall() && fh.getY1() != fh.getY2())
        {
            double s1 = Math.Abs(fh.getY2() - fh.getY1());
            double s2 = Math.Abs(fh.getX2() - fh.getX1());
            double s5 = Math.Cos(Math.Atan(s2 / s1)) * (Math.Abs(initial.X - fh.getX1()) / Math.Cos(Math.Atan(s1 / s2)));
            if (fh.getY2() < fh.getY1())
            {
                dropY = fh.getY1() - (int)s5;
            }
            else
            {
                dropY = fh.getY1() + (int)s5;
            }
        }
        return new Point(initial.X, dropY);
    }

    public void generateMapDropRangeCache()
    {
        Monitor.Enter(bndLock);
        try
        {
            var bounds = dropBoundsCache.GetValueOrDefault(mapid);

            if (bounds != null)
            {
                xLimits = bounds.Value;
            }
            else
            {
                // assuming MINIMAP always have an equal-greater picture representation of the map area (players won't walk beyond the area known by the minimap).
                Point lp = new Point(mapArea.X, mapArea.Y);
                Point rp = new Point(mapArea.X + mapArea.Width, mapArea.Y);
                Point fallback = new Point(mapArea.X + (mapArea.Width / 2), mapArea.Y);

                lp = bsearchDropPos(lp, fallback);  // approximated leftmost fh node position
                rp = bsearchDropPos(rp, fallback);  // approximated rightmost fh node position

                xLimits = new(lp.X + 14, rp.X - 14);
                dropBoundsCache.Add(mapid, xLimits);
            }
        }
        finally
        {
            Monitor.Exit(bndLock);
        }
    }

    private Point bsearchDropPos(Point initial, Point fallback)
    {
        Point? res;
        Point? dropPos = null;

        int awayx = fallback.X;
        int homex = initial.X;

        int y = initial.Y - 85;

        do
        {
            int distx = awayx - homex;
            int dx = distx / 2;

            int searchx = homex + dx;
            if ((res = calcPointBelow(new Point(searchx, y))) != null)
            {
                awayx = searchx;
                dropPos = res;
            }
            else
            {
                homex = searchx;
            }
        } while (Math.Abs(homex - awayx) > 5);

        return (dropPos != null) ? dropPos.Value : fallback;
    }

    public Point calcDropPos(Point initial, Point fallback)
    {
        if (initial.X < xLimits.Key)
        {
            initial.X = xLimits.Key;
        }
        else if (initial.X > xLimits.Value)
        {
            initial.X = xLimits.Value;
        }

        Point? ret = calcPointBelow(new Point(initial.X, initial.Y - 85));   // actual drop ranges: default - 120, explosive - 360
        if (ret == null)
        {
            ret = bsearchDropPos(initial, fallback);
        }

        if (!mapArea.Contains(ret.Value))
        { // found drop pos outside the map :O
            return fallback;
        }

        return ret.Value;
    }

    public bool canDeployDoor(Point pos)
    {
        Point? toStep = calcPointBelow(pos);
        return toStep != null && toStep.Value.distance(pos) <= 42;
    }

    /**
     * Fetches angle relative between spawn and door points where 3 O'Clock is 0
     * and 12 O'Clock is 270 degrees
     *
     * @param spawnPoint
     * @param doorPoint
     * @return angle in degress from 0-360.
     */
    private static double getAngle(Point doorPoint, Point spawnPoint)
    {
        double dx = doorPoint.X - spawnPoint.X;
        // Minus to correct for coord re-mapping
        double dy = -(doorPoint.Y - spawnPoint.Y);

        double inRads = Math.Atan2(dy, dx);

        // We need to map to coord system when 0 degree is at 3 O'clock, 270 at 12 O'clock
        if (inRads < 0)
        {
            inRads = Math.Abs(inRads);
        }
        else
        {
            inRads = 2 * Math.PI - inRads;
        }

        return (180 / Math.PI) * inRads;
    }

    /**
     * Converts angle in degrees to rounded cardinal coordinate.
     *
     * @param angle
     * @return correspondent coordinate.
     */
    public static string getRoundedCoordinate(double angle)
    {
        string[] directions = { "E", "SE", "S", "SW", "W", "NW", "N", "NE", "E" };
        return directions[(int)Math.Round(((angle % 360) / 45))];
    }

    public KeyValuePair<string, int>? getDoorPositionStatus(Point pos)
    {
        Portal? portal = findClosestPlayerSpawnpoint(pos);

        double angle = getAngle(portal.getPosition(), pos);
        double distn = pos.distanceSq(portal.getPosition());

        if (distn <= 777777.7)
        {
            return null;
        }

        distn = Math.Sqrt(distn);
        return new(getRoundedCoordinate(angle), (int)distn);
    }

    private static void sortDropEntries(List<MonsterDropEntry> from, List<MonsterDropEntry> item, List<MonsterDropEntry> visibleQuest, List<MonsterDropEntry> otherQuest, IPlayer chr)
    {
        ItemInformationProvider ii = ItemInformationProvider.getInstance();

        foreach (MonsterDropEntry mde in from)
        {
            if (!ii.isQuestItem(mde.itemId))
            {
                item.Add(mde);
            }
            else
            {
                if (chr.needQuestItem(mde.questid, mde.itemId))
                {
                    visibleQuest.Add(mde);
                }
                else
                {
                    otherQuest.Add(mde);
                }
            }
        }
    }

    public byte dropItemsFromMonsterOnMap(List<MonsterDropEntry> dropEntry, Point pos, byte d, int chRate, byte droptype, int mobpos, IPlayer chr, Monster mob)
    {
        if (dropEntry.Count == 0)
        {
            return d;
        }

        Collections.shuffle(dropEntry);

        Item idrop;
        ItemInformationProvider ii = ItemInformationProvider.getInstance();

        foreach (MonsterDropEntry de in dropEntry)
        {
            float cardRate = chr.getCardRate(de.itemId);
            int dropChance = (int)Math.Min((float)de.chance * chRate * cardRate, int.MaxValue);

            if (Randomizer.nextInt(999999) < dropChance)
            {
                if (droptype == 3)
                {
                    pos.X = mobpos + ((d % 2 == 0) ? (40 * ((d + 1) / 2)) : -(40 * (d / 2)));
                }
                else
                {
                    pos.X = mobpos + ((d % 2 == 0) ? (25 * ((d + 1) / 2)) : -(25 * (d / 2)));
                }
                if (de.itemId == 0)
                { // meso
                    int mesos = Randomizer.nextInt(de.Maximum - de.Minimum) + de.Minimum;

                    if (mesos > 0)
                    {
                        if (chr.getBuffedValue(BuffStat.MESOUP) != null)
                        {
                            mesos = (int)(mesos * chr.getBuffedValue(BuffStat.MESOUP)!.Value / 100.0);
                        }
                        mesos = mesos * chr.getMesoRate();
                        if (mesos <= 0)
                        {
                            mesos = int.MaxValue;
                        }

                        spawnMesoDrop(mesos, calcDropPos(pos, mob.getPosition()), mob, chr, false, droptype);
                    }
                }
                else
                {
                    if (ItemConstants.getInventoryType(de.itemId) == InventoryType.EQUIP)
                    {
                        idrop = ii.randomizeStats((Equip)ii.getEquipById(de.itemId));
                    }
                    else
                    {
                        idrop = new Item(de.itemId, 0, (short)(de.Maximum != 1 ? Randomizer.nextInt(de.Maximum - de.Minimum) + de.Minimum : 1));
                    }
                    spawnDrop(idrop, calcDropPos(pos, mob.getPosition()), mob, chr, droptype, de.questid);
                }
                d++;
            }
        }

        return d;
    }

    public byte dropGlobalItemsFromMonsterOnMap(List<MonsterGlobalDropEntry> globalEntry, Point pos, byte d, byte droptype, int mobpos, IPlayer chr, Monster mob)
    {
        Collections.shuffle(globalEntry);

        Item idrop;
        ItemInformationProvider ii = ItemInformationProvider.getInstance();

        foreach (MonsterGlobalDropEntry de in globalEntry)
        {
            if (Randomizer.nextInt(999999) < de.chance)
            {
                if (droptype == 3)
                {
                    pos.X = mobpos + (d % 2 == 0 ? (40 * (d + 1) / 2) : -(40 * (d / 2)));
                }
                else
                {
                    pos.X = mobpos + ((d % 2 == 0) ? (25 * (d + 1) / 2) : -(25 * (d / 2)));
                }
                if (de.itemId != 0)
                {
                    if (ItemConstants.getInventoryType(de.itemId) == InventoryType.EQUIP)
                    {
                        idrop = ii.randomizeStats((Equip)ii.getEquipById(de.itemId));
                    }
                    else
                    {
                        idrop = new Item(de.itemId, 0, (short)(de.Maximum != 1 ? Randomizer.nextInt(de.Maximum - de.Minimum) + de.Minimum : 1));
                    }
                    spawnDrop(idrop, calcDropPos(pos, mob.getPosition()), mob, chr, droptype, de.questid);
                    d++;
                }
            }
        }

        return d;
    }

    private void dropFromMonster(IPlayer chr, Monster mob, bool useBaseRate)
    {
        if (mob.dropsDisabled() || !dropsOn)
        {
            return;
        }

        byte droptype = (byte)(mob.getStats().isExplosiveReward() ? 3 : mob.getStats().isFfaLoot() ? 2 : chr.getParty() != null ? 1 : 0);
        int mobpos = mob.getPosition().X;
        int chRate = !mob.isBoss() ? chr.getDropRate() : chr.getBossDropRate();
        Point pos = new Point(0, mob.getPosition().Y);

        var stati = mob.getStati(MonsterStatus.SHOWDOWN);
        if (stati != null)
        {
            chRate *= (int)(stati.getStati().get(MonsterStatus.SHOWDOWN)!.Value / 100.0 + 1.0);
        }

        if (useBaseRate)
        {
            chRate = 1;
        }

        MonsterInformationProvider mi = MonsterInformationProvider.getInstance();
        List<MonsterGlobalDropEntry> globalEntry = mi.getRelevantGlobalDrops(this.getId());

        List<MonsterDropEntry> dropEntry = new();
        List<MonsterDropEntry> visibleQuestEntry = new();
        List<MonsterDropEntry> otherQuestEntry = new();

        List<MonsterDropEntry> lootEntry = YamlConfig.config.server.USE_SPAWN_RELEVANT_LOOT ? mob.retrieveRelevantDrops() : mi.retrieveEffectiveDrop(mob.getId());
        sortDropEntries(lootEntry, dropEntry, visibleQuestEntry, otherQuestEntry, chr);     // thanks Articuno, Limit, Rohenn for noticing quest loots not showing up in only-quest item drops scenario

        if (lootEntry.Count == 0)
        {   // thanks resinate
            return;
        }

        registerMobItemDrops(droptype, mobpos, chRate, pos, dropEntry, visibleQuestEntry, otherQuestEntry, globalEntry, chr, mob);
    }

    public void dropItemsFromMonster(List<MonsterDropEntry> list, IPlayer chr, Monster mob)
    {
        if (mob.dropsDisabled() || !dropsOn)
        {
            return;
        }

        byte droptype = (byte)(chr.getParty() != null ? 1 : 0);
        int mobpos = mob.getPosition().X;
        int chRate = 1000000;   // guaranteed item drop
        byte d = 1;
        Point pos = new Point(0, mob.getPosition().Y);

        dropItemsFromMonsterOnMap(list, pos, d, chRate, droptype, mobpos, chr, mob);
    }

    public void dropFromFriendlyMonster(IPlayer chr, Monster mob)
    {
        dropFromMonster(chr, mob, true);
    }

    public void dropFromReactor(IPlayer chr, Reactor reactor, Item drop, Point dropPos, short questid)
    {
        spawnDrop(drop, this.calcDropPos(dropPos, reactor.getPosition()), reactor, chr, (byte)(chr.getParty() != null ? 1 : 0), questid);
    }

    private void stopItemMonitor()
    {
        itemMonitor?.cancel(false);
        itemMonitor = null;

        expireItemsTask?.cancel(false);
        expireItemsTask = null;

        if (YamlConfig.config.server.USE_SPAWN_LOOT_ON_ANIMATION)
        {
            mobSpawnLootTask?.cancel(false);
            mobSpawnLootTask = null;
        }

        characterStatUpdateTask?.cancel(false);
        characterStatUpdateTask = null;
    }

    private void cleanItemMonitor()
    {
        objectLock.EnterWriteLock();
        try
        {
            registeredDrops.RemoveAll(x => x == null);
        }
        finally
        {
            objectLock.ExitWriteLock();
        }
    }

    private void startItemMonitor()
    {
        chrLock.EnterWriteLock();
        try
        {
            if (itemMonitor != null)
            {
                return;
            }

            itemMonitor = TimerManager.getInstance().register(() =>
            {
                chrLock.EnterWriteLock();
                try
                {
                    if (characters.Count == 0)
                    {
                        if (itemMonitorTimeout == 0)
                        {
                            if (itemMonitor != null)
                            {
                                stopItemMonitor();
                                aggroMonitor.stopAggroCoordinator();
                            }

                            return;
                        }
                        else
                        {
                            itemMonitorTimeout--;
                        }
                    }
                    else
                    {
                        itemMonitorTimeout = 1;
                    }
                }
                finally
                {
                    chrLock.ExitWriteLock();
                }

                bool tryClean;
                objectLock.EnterReadLock();
                try
                {
                    tryClean = registeredDrops.Count > 70;
                }
                finally
                {
                    objectLock.ExitReadLock();
                }

                if (tryClean)
                {
                    cleanItemMonitor();
                }
            }, YamlConfig.config.server.ITEM_MONITOR_TIME, YamlConfig.config.server.ITEM_MONITOR_TIME);

            expireItemsTask = TimerManager.getInstance().register(() => { makeDisappearExpiredItemDrops(); }, YamlConfig.config.server.ITEM_EXPIRE_CHECK, YamlConfig.config.server.ITEM_EXPIRE_CHECK);

            if (YamlConfig.config.server.USE_SPAWN_LOOT_ON_ANIMATION)
            {
                Monitor.Enter(lootLock);
                try
                {
                    mobLootEntries.Clear();
                }
                finally
                {
                    Monitor.Exit(lootLock);
                }

                mobSpawnLootTask = TimerManager.getInstance().register(() => spawnMobItemDrops(), 200, 200);
            }

            characterStatUpdateTask = TimerManager.getInstance().register(runCharacterStatUpdate, 200, 200);

            itemMonitorTimeout = 1;
        }
        finally
        {
            chrLock.ExitWriteLock();
        }
    }

    private bool hasItemMonitor()
    {
        chrLock.EnterReadLock();
        try
        {
            return itemMonitor != null;
        }
        finally
        {
            chrLock.ExitReadLock();
        }
    }

    public int getDroppedItemCount()
    {
        return droppedItemCount.get();
    }

    private void instantiateItemDrop(MapItem mdrop)
    {
        if (droppedItemCount.get() >= YamlConfig.config.server.ITEM_LIMIT_ON_MAP)
        {
            IMapObject? mapobj;

            do
            {
                mapobj = null;

                objectLock.EnterWriteLock();
                try
                {
                    while (mapobj == null)
                    {
                        if (registeredDrops.Count == 0)
                        {
                            break;
                        }
                        var item = registeredDrops[0];
                        registeredDrops.RemoveAt(0);
                        if (item?.TryGetTarget(out var d) ?? false)
                            mapobj = d;
                    }
                }
                finally
                {
                    objectLock.ExitWriteLock();
                }
            } while (!makeDisappearItemFromMap(mapobj));
        }

        objectLock.EnterWriteLock();
        try
        {
            registerItemDrop(mdrop);
            registeredDrops.Add(new(mdrop));
        }
        finally
        {
            objectLock.ExitWriteLock();
        }

        droppedItemCount.incrementAndGet();
    }

    private void registerItemDrop(MapItem mdrop)
    {
        droppedItems.AddOrUpdate(mdrop, !everlast ? Server.getInstance().getCurrentTime() + YamlConfig.config.server.ITEM_EXPIRE_TIME : long.MaxValue);
    }

    public void unregisterItemDrop(MapItem mdrop)
    {
        objectLock.EnterWriteLock();
        try
        {
            droppedItems.Remove(mdrop);
        }
        finally
        {
            objectLock.ExitWriteLock();
        }
    }

    private void makeDisappearExpiredItemDrops()
    {
        List<MapItem> toDisappear = new();

        objectLock.EnterReadLock();
        try
        {
            long timeNow = Server.getInstance().getCurrentTime();

            foreach (var it in droppedItems)
            {
                if (it.Value < timeNow)
                {
                    toDisappear.Add(it.Key);
                }
            }
        }
        finally
        {
            objectLock.ExitReadLock();
        }

        foreach (MapItem mmi in toDisappear)
        {
            makeDisappearItemFromMap(mmi);
        }

        objectLock.EnterWriteLock();
        try
        {
            foreach (MapItem mmi in toDisappear)
            {
                droppedItems.Remove(mmi);
            }
        }
        finally
        {
            objectLock.ExitWriteLock();
        }
    }

    private void registerMobItemDrops(byte droptype, int mobpos, int chRate, Point pos, List<MonsterDropEntry> dropEntry, List<MonsterDropEntry> visibleQuestEntry, List<MonsterDropEntry> otherQuestEntry, List<MonsterGlobalDropEntry> globalEntry, IPlayer chr, Monster mob)
    {
        MobLootEntry mle = new MobLootEntry(this, droptype, mobpos, chRate, pos, dropEntry, visibleQuestEntry, otherQuestEntry, globalEntry, chr, mob);

        if (YamlConfig.config.server.USE_SPAWN_LOOT_ON_ANIMATION)
        {
            int animationTime = mob.getAnimationTime("die1");

            Monitor.Enter(lootLock);
            try
            {
                long timeNow = Server.getInstance().getCurrentTime();
                mobLootEntries.AddOrUpdate(mle, timeNow + ((long)(0.42 * animationTime)));
            }
            finally
            {
                Monitor.Exit(lootLock);
            }
        }
        else
        {
            mle.run();
        }
    }

    private void spawnMobItemDrops()
    {
        HashSet<KeyValuePair<MobLootEntry, long>> mleList;

        Monitor.Enter(lootLock);
        try
        {
            mleList = new(mobLootEntries);
        }
        finally
        {
            Monitor.Exit(lootLock);
        }

        long timeNow = Server.getInstance().getCurrentTime();
        List<MobLootEntry> toRemove = new();
        foreach (var mlee in mleList)
        {
            if (mlee.Value < timeNow)
            {
                toRemove.Add(mlee.Key);
            }
        }

        if (toRemove.Count > 0)
        {
            List<MobLootEntry> toSpawnLoot = new();

            Monitor.Enter(lootLock);
            try
            {
                foreach (MobLootEntry mle in toRemove)
                {
                    if (mobLootEntries.Remove(mle, out var mler))
                    {
                        toSpawnLoot.Add(mle);
                    }
                }
            }
            finally
            {
                Monitor.Exit(lootLock);
            }

            foreach (MobLootEntry mle in toSpawnLoot)
            {
                mle.run();
            }
        }
    }

    private List<MapItem> getDroppedItems()
    {
        objectLock.EnterReadLock();
        try
        {
            return new(droppedItems.Keys);
        }
        finally
        {
            objectLock.ExitReadLock();
        }
    }

    public int getDroppedItemsCountById(int itemid)
    {
        int count = 0;
        foreach (MapItem mmi in getDroppedItems())
        {
            if (mmi.getItemId() == itemid)
            {
                count++;
            }
        }

        return count;
    }

    public void pickItemDrop(Packet pickupPacket, MapItem mdrop)
    { // mdrop must be already locked and not-pickedup checked at this point
        broadcastMessage(pickupPacket, mdrop.getPosition());

        droppedItemCount.decrementAndGet();
        this.removeMapObject(mdrop);
        mdrop.setPickedUp(true);
        unregisterItemDrop(mdrop);
    }

    public List<MapItem> updatePlayerItemDropsToParty(int partyid, int charid, List<IPlayer> partyMembers, IPlayer? partyLeaver)
    {
        List<MapItem> partyDrops = new();

        foreach (MapItem mdrop in getDroppedItems())
        {
            if (mdrop.getOwnerId() == charid)
            {
                mdrop.lockItem();
                try
                {
                    if (mdrop.isPickedUp())
                    {
                        continue;
                    }

                    mdrop.setPartyOwnerId(partyid);

                    Packet removePacket = PacketCreator.silentRemoveItemFromMap(mdrop.getObjectId());
                    Packet updatePacket = PacketCreator.updateMapItemObject(mdrop, partyLeaver == null);

                    foreach (IPlayer mc in partyMembers)
                    {
                        if (this.Equals(mc.getMap()))
                        {
                            mc.sendPacket(removePacket);

                            if (mc.needQuestItem(mdrop.getQuest(), mdrop.getItemId()))
                            {
                                mc.sendPacket(updatePacket);
                            }
                        }
                    }

                    if (partyLeaver != null)
                    {
                        if (this.Equals(partyLeaver.getMap()))
                        {
                            partyLeaver.sendPacket(removePacket);

                            if (partyLeaver.needQuestItem(mdrop.getQuest(), mdrop.getItemId()))
                            {
                                partyLeaver.sendPacket(PacketCreator.updateMapItemObject(mdrop, true));
                            }
                        }
                    }
                }
                finally
                {
                    mdrop.unlockItem();
                }
            }
            else if (partyid != -1 && mdrop.getPartyOwnerId() == partyid)
            {
                partyDrops.Add(mdrop);
            }
        }

        return partyDrops;
    }

    public void updatePartyItemDropsToNewcomer(IPlayer newcomer, List<MapItem> partyItems)
    {
        foreach (MapItem mdrop in partyItems)
        {
            mdrop.lockItem();
            try
            {
                if (mdrop.isPickedUp())
                {
                    continue;
                }

                Packet removePacket = PacketCreator.silentRemoveItemFromMap(mdrop.getObjectId());
                Packet updatePacket = PacketCreator.updateMapItemObject(mdrop, true);

                if (newcomer != null)
                {
                    if (this.Equals(newcomer.getMap()))
                    {
                        newcomer.sendPacket(removePacket);

                        if (newcomer.needQuestItem(mdrop.getQuest(), mdrop.getItemId()))
                        {
                            newcomer.sendPacket(updatePacket);
                        }
                    }
                }
            }
            finally
            {
                mdrop.unlockItem();
            }
        }
    }

    private void spawnDrop(Item idrop, Point dropPos, IMapObject dropper, IPlayer chr, byte droptype, short questid)
    {
        MapItem mdrop = new MapItem(idrop, dropPos, dropper, chr, droptype, false, questid);
        mdrop.setDropTime(Server.getInstance().getCurrentTime());
        spawnAndAddRangedMapObject(mdrop, c =>
        {
            var chr1 = c.OnlinedCharacter;

            if (chr1.needQuestItem(questid, idrop.getItemId()))
            {
                mdrop.lockItem();
                try
                {
                    c.sendPacket(PacketCreator.dropItemFromMapObject(chr1, mdrop, dropper.getPosition(), dropPos, 1));
                }
                finally
                {
                    mdrop.unlockItem();
                }
            }
        }, null);

        instantiateItemDrop(mdrop);
        activateItemReactors(mdrop, chr.getClient());
    }

    public void spawnMesoDrop(int meso, Point position, IMapObject dropper, IPlayer owner, bool playerDrop, byte droptype)
    {
        Point droppos = calcDropPos(position, position);
        MapItem mdrop = new MapItem(meso, droppos, dropper, owner, droptype, playerDrop);
        mdrop.setDropTime(Server.getInstance().getCurrentTime());

        spawnAndAddRangedMapObject(mdrop, c =>
        {
            mdrop.lockItem();
            try
            {
                c.sendPacket(PacketCreator.dropItemFromMapObject(c.OnlinedCharacter, mdrop, dropper.getPosition(), droppos, 1));
            }
            finally
            {
                mdrop.unlockItem();
            }
        }, null);

        instantiateItemDrop(mdrop);
    }

    public void disappearingItemDrop(IMapObject dropper, IPlayer owner, Item item, Point pos)
    {
        Point droppos = calcDropPos(pos, pos);
        MapItem mdrop = new MapItem(item, droppos, dropper, owner, 1, false);

        mdrop.lockItem();
        try
        {
            broadcastItemDropMessage(mdrop, dropper.getPosition(), droppos, 3, mdrop.getPosition());
        }
        finally
        {
            mdrop.unlockItem();
        }
    }

    public void disappearingMesoDrop(int meso, IMapObject dropper, IPlayer owner, Point pos)
    {
        Point droppos = calcDropPos(pos, pos);
        MapItem mdrop = new MapItem(meso, droppos, dropper, owner, 1, false);

        mdrop.lockItem();
        try
        {
            broadcastItemDropMessage(mdrop, dropper.getPosition(), droppos, 3, mdrop.getPosition());
        }
        finally
        {
            mdrop.unlockItem();
        }
    }

    public Monster? getMonsterById(int id)
    {
        objectLock.EnterReadLock();
        try
        {
            return getMapObjects().FirstOrDefault(x =>
            {
                if (x.getType() == MapObjectType.MONSTER && x is Monster obj)
                {
                    return obj.getId() == id;
                }
                return false;
            }) as Monster;

        }
        finally
        {
            objectLock.ExitReadLock();
        }
    }

    public int countMonster(int id)
    {
        return countMonster(id, id);
    }

    public int countMonster(int minid, int maxid)
    {
        int count = 0;
        foreach (IMapObject m in getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.MONSTER)))
        {
            Monster mob = (Monster)m;
            if (mob.getId() >= minid && mob.getId() <= maxid)
            {
                count++;
            }
        }
        return count;
    }

    public int countMonsters()
    {
        return getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.MONSTER)).Count;
    }

    public int countReactors()
    {
        return getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.REACTOR)).Count;
    }

    public List<IMapObject> getReactors()
    {
        return getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.REACTOR));
    }

    public List<IMapObject> getMonsters()
    {
        return getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.MONSTER));
    }

    public List<Reactor> getAllReactors()
    {
        return getReactors().OfType<Reactor>().ToList();

    }

    public List<Monster> getAllMonsters()
    {
        return getMonsters().OfType<Monster>().ToList();
    }

    public int countItems()
    {
        return getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.ITEM)).Count;
    }

    public List<IMapObject> getItems()
    {
        return getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.ITEM));
    }

    public int countPlayers()
    {
        return getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.PLAYER)).Count;
    }

    public List<IMapObject> getPlayers()
    {
        return getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.PLAYER));
    }

    public List<IPlayer> getAllPlayers()
    {
        List<IPlayer> character;
        chrLock.EnterReadLock();
        try
        {
            character = new(characters);
        }
        finally
        {
            chrLock.ExitReadLock();
        }

        return character;
    }

    public Dictionary<int, IPlayer> getMapAllPlayers()
    {
        Dictionary<int, IPlayer> pchars = new();
        foreach (IPlayer chr in this.getAllPlayers())
        {
            pchars.AddOrUpdate(chr.getId(), chr);
        }

        return pchars;
    }

    public List<IPlayer> getPlayersInRange(Rectangle box)
    {
        List<IPlayer> character = new();
        chrLock.EnterReadLock();
        try
        {
            foreach (IPlayer chr in characters)
            {
                if (box.Contains(chr.getPosition()))
                {
                    character.Add(chr);
                }
            }
        }
        finally
        {
            chrLock.ExitReadLock();
        }

        return character;
    }

    public int countAlivePlayers()
    {
        return getAllPlayers().Count(x => x.isAlive());
    }

    public int countBosses()
    {
        return getAllMonsters().Count(x => x.isBoss());
    }

    public bool damageMonster(IPlayer chr, Monster monster, int damage)
    {
        if (monster.getId() == MobId.ZAKUM_1)
        {
            foreach (IMapObject mapObj in chr.getMap().getMapObjects())
            {
                Monster? mons = chr.getMap().getMonsterByOid(mapObj.getObjectId());
                if (mons != null)
                {
                    if (mons.getId() >= MobId.ZAKUM_ARM_1 && mons.getId() <= MobId.ZAKUM_ARM_8)
                    {
                        return true;
                    }
                }
            }
        }
        if (monster.isAlive())
        {
            bool killed = monster.damage(chr, damage, false);

            var selfDestr = monster.getStats().selfDestruction();
            if (selfDestr != null && selfDestr.getHp() > -1)
            {
                // should work ;p
                if (monster.getHp() <= selfDestr.getHp())
                {
                    killMonster(monster, chr, true, selfDestr.getAction());
                    return true;
                }
            }
            if (killed)
            {
                killMonster(monster, chr, true);
            }
            return true;
        }
        return false;
    }

    public void broadcastBalrogVictory(string leaderName)
    {
        getWorldServer().dropMessage(6, "[Victory] " + leaderName + "'s party has successfully defeated the Balrog! Praise to them, they finished with " + countAlivePlayers() + " players alive.");
    }

    public void broadcastHorntailVictory()
    {
        getWorldServer().dropMessage(6, "[Victory] To the crew that have finally conquered Horned Tail after numerous attempts, I salute thee! You are the true heroes of Leafre!!");
    }

    public void broadcastZakumVictory()
    {
        getWorldServer().dropMessage(6, "[Victory] At last, the tree of evil that for so long overwhelmed Ossyria has fallen. To the crew that managed to finally conquer Zakum, after numerous attempts, victory! You are the true heroes of Ossyria!!");
    }

    public void broadcastPinkBeanVictory(int channel)
    {
        getWorldServer().dropMessage(6, "[Victory] In a swift stroke of sorts, the crew that has attempted Pink Bean at channel " + channel + " has ultimately defeated it. The Temple of Time shines radiantly once again, the day finally coming back, as the crew that managed to finally conquer it returns victoriously from the battlefield!!");
    }

    private bool removeKilledMonsterObject(Monster monster)
    {
        monster.lockMonster();
        try
        {
            if (monster.getHp() < 0)
            {
                return false;
            }

            spawnedMonstersOnMap.decrementAndGet();
            removeMapObject(monster);
            monster.disposeMapObject();
            if (monster.hasBossHPBar())
            {
                // thanks resinate for noticing boss HPbar not clearing after mob defeat in certain scenarios
                broadcastBossHpMessage(monster, monster.GetHashCode(), monster.makeBossHPBarPacket(), monster.getPosition());
            }

            return true;
        }
        finally
        {
            monster.unlockMonster();
        }
    }

    public void killMonster(Monster monster, IPlayer? chr, bool withDrops)
    {
        killMonster(monster, chr, withDrops, 1);
    }

    public void killMonster(Monster monster, IPlayer? chr, bool withDrops, int animation)
    {
        if (monster == null)
        {
            return;
        }

        if (chr == null)
        {
            if (removeKilledMonsterObject(monster))
            {
                monster.dispatchMonsterKilled(false);
                broadcastMessage(PacketCreator.killMonster(monster.getObjectId(), animation), monster.getPosition());
                monster.aggroSwitchController(null, false);
            }
        }
        else
        {
            if (removeKilledMonsterObject(monster))
            {
                try
                {
                    if (monster.getStats().getLevel() >= chr.getLevel() + 30 && !chr.isGM())
                    {
                        AutobanFactory.GENERAL.alert(chr, " for killing a " + monster.getName() + " which is over 30 levels higher.");
                    }

                    /*if (chr.getQuest(Quest.getInstance(29400)).getStatus().Equals(QuestStatus.Status.STARTED)) {
                     if (chr.getLevel() >= 120 && monster.getStats().getLevel() >= 120) {
                     //FIX MEDAL SHET
                     } else if (monster.getStats().getLevel() >= chr.getLevel()) {
                     }
                     }*/

                    if (monster.getCP() > 0 && chr.getMap().isCPQMap())
                    {
                        chr.gainCP(monster.getCP());
                    }

                    int buff = monster.getBuffToGive();
                    if (buff > -1)
                    {
                        ItemInformationProvider mii = ItemInformationProvider.getInstance();
                        foreach (IMapObject mmo in this.getPlayers())
                        {
                            IPlayer character = (IPlayer)mmo;
                            if (character.isAlive())
                            {
                                var statEffect = mii.getItemEffect(buff)!;
                                character.sendPacket(PacketCreator.showOwnBuffEffect(buff, 1));
                                broadcastMessage(character, PacketCreator.showBuffEffect(character.getId(), buff, 1), false);
                                statEffect.applyTo(character);
                            }
                        }
                    }

                    if (MobId.isZakumArm(monster.getId()))
                    {
                        bool makeZakReal = true;
                        var objects = getMapObjects();
                        foreach (IMapObject mapObj in objects)
                        {
                            Monster? mons = getMonsterByOid(mapObj.getObjectId());
                            if (mons != null)
                            {
                                if (MobId.isZakumArm(mons.getId()))
                                {
                                    makeZakReal = false;
                                    break;
                                }
                            }
                        }
                        if (makeZakReal)
                        {
                            var map = chr.getMap();

                            foreach (IMapObject mapObj in objects)
                            {
                                Monster? mons = map.getMonsterByOid(mapObj.getObjectId());
                                if (mons != null)
                                {
                                    if (mons.getId() == MobId.ZAKUM_1)
                                    {
                                        makeMonsterReal(mons);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    var dropOwner = monster.killBy(chr);
                    if (withDrops && !monster.dropsDisabled())
                    {
                        if (dropOwner == null)
                        {
                            dropOwner = chr;
                        }
                        dropFromMonster(dropOwner, monster, false);
                    }

                    if (monster.hasBossHPBar())
                    {
                        foreach (IPlayer mc in this.getAllPlayers())
                        {
                            if (mc.getTargetHpBarHash() == monster.GetHashCode())
                            {
                                mc.resetPlayerAggro();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    log.Error(e.ToString());
                }
                finally
                {     // thanks resinate for pointing out a memory leak possibly from an exception thrown
                    monster.dispatchMonsterKilled(true);
                    broadcastMessage(PacketCreator.killMonster(monster.getObjectId(), animation), monster.getPosition());
                }
            }
        }
    }

    public void killFriendlies(Monster mob)
    {
        this.killMonster(mob, (IPlayer?)getPlayers().ElementAtOrDefault(0), false);
    }

    public void killMonster(int mobId)
    {
        IPlayer? chr = (IPlayer?)getPlayers().ElementAtOrDefault(0);
        List<Monster> mobList = getAllMonsters();

        foreach (Monster mob in mobList)
        {
            if (mob.getId() == mobId)
            {
                this.killMonster(mob, chr, false);
            }
        }
    }

    public void killMonsterWithDrops(int mobId)
    {
        Dictionary<int, IPlayer> mapChars = this.getMapPlayers();

        if (mapChars.Count > 0)
        {
            IPlayer defaultChr = mapChars.FirstOrDefault().Value;
            List<Monster> mobList = getAllMonsters();

            foreach (Monster mob in mobList)
            {
                if (mob.getId() == mobId)
                {
                    var chr = mapChars.GetValueOrDefault(mob.getHighestDamagerId());
                    if (chr == null)
                    {
                        chr = defaultChr;
                    }

                    this.killMonster(mob, chr, true);
                }
            }
        }
    }

    public void softKillAllMonsters()
    {
        closeMapSpawnPoints();

        foreach (IMapObject monstermo in getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.MONSTER)))
        {
            Monster monster = (Monster)monstermo;
            if (monster.getStats().isFriendly())
            {
                continue;
            }

            if (removeKilledMonsterObject(monster))
            {
                monster.dispatchMonsterKilled(false);
            }
        }
    }

    public void killAllMonstersNotFriendly()
    {
        closeMapSpawnPoints();

        foreach (IMapObject monstermo in getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.MONSTER)))
        {
            Monster monster = (Monster)monstermo;
            if (monster.getStats().isFriendly())
            {
                continue;
            }

            killMonster(monster, null, false, 1);
        }
    }

    public void killAllMonsters()
    {
        closeMapSpawnPoints();

        foreach (IMapObject monstermo in getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.MONSTER)))
        {
            Monster monster = (Monster)monstermo;

            killMonster(monster, null, false, 1);
        }
    }

    public void destroyReactors(int first, int last)
    {
        List<Reactor> toDestroy = new();
        var reactors = getReactors();

        foreach (IMapObject obj in reactors)
        {
            Reactor mr = (Reactor)obj;
            if (mr.getId() >= first && mr.getId() <= last)
            {
                toDestroy.Add(mr);
            }
        }

        foreach (Reactor mr in toDestroy)
        {
            destroyReactor(mr.getObjectId());
        }
    }

    public void destroyReactor(int oid)
    {
        var reactor = getReactorByOid(oid);

        if (reactor != null)
        {
            if (reactor.destroy())
            {
                removeMapObject(reactor);
            }
        }
    }

    public void resetReactors()
    {
        List<Reactor> list = new();

        objectLock.EnterReadLock();
        try
        {
            foreach (IMapObject o in getMapObjects())
            {
                if (o.getType() == MapObjectType.REACTOR)
                {
                    Reactor r = ((Reactor)o);
                    list.Add(r);
                }
            }
        }
        finally
        {
            objectLock.ExitReadLock();
        }

        resetReactors(list);
    }

    public void resetReactors(List<Reactor> list)
    {
        foreach (Reactor r in list)
        {
            if (r.forceDelayedRespawn())
            {  // thanks Conrad for suggesting reactor with delay respawning immediately
                continue;
            }

            r.lockReactor();
            try
            {
                r.resetReactorActions(0);
                r.setAlive(true);
                broadcastMessage(PacketCreator.triggerReactor(r, 0));
            }
            finally
            {
                r.unlockReactor();
            }
        }
    }

    public void shuffleReactors()
    {
        List<Point> points = new();
        objectLock.EnterReadLock();
        try
        {
            foreach (IMapObject o in mapobjects.Values)
            {
                if (o.getType() == MapObjectType.REACTOR)
                {
                    points.Add(o.getPosition());
                }
            }
            Collections.shuffle(points);
            foreach (IMapObject o in mapobjects.Values)
            {
                if (o.getType() == MapObjectType.REACTOR)
                {
                    o.setPosition(points.remove(points.Count - 1));
                }
            }
        }
        finally
        {
            objectLock.ExitReadLock();
        }
    }

    public void shuffleReactors(int first, int last)
    {
        List<Point> points = new();
        var reactors = getReactors();
        List<IMapObject> targets = new();

        foreach (var obj in reactors)
        {
            Reactor mr = (Reactor)obj;
            if (mr.getId() >= first && mr.getId() <= last)
            {
                points.Add(mr.getPosition());
                targets.Add(obj);
            }
        }
        Collections.shuffle(points);
        foreach (var obj in targets)
        {
            Reactor mr = (Reactor)obj;
            mr.setPosition(points.remove(points.Count - 1));
        }
    }

    public void shuffleReactors(List<object> list)
    {
        List<Point> points = new();
        List<IMapObject> listObjects = new();
        List<IMapObject> targets = new();

        objectLock.EnterReadLock();
        try
        {
            foreach (object ob in list)
            {
                if (ob is IMapObject mmo)
                {

                    if (mapobjects.ContainsValue(mmo) && mmo.getType() == MapObjectType.REACTOR)
                    {
                        listObjects.Add(mmo);
                    }
                }
            }
        }
        finally
        {
            objectLock.ExitReadLock();
        }

        foreach (var obj in listObjects)
        {
            Reactor mr = (Reactor)obj;

            points.Add(mr.getPosition());
            targets.Add(obj);
        }
        Collections.shuffle(points);
        foreach (var obj in targets)
        {
            Reactor mr = (Reactor)obj;
            mr.setPosition(points.remove(points.Count - 1));
        }
    }

    private Dictionary<int, IMapObject> getCopyMapObjects()
    {
        objectLock.EnterReadLock();
        try
        {
            return new(mapobjects);
        }
        finally
        {
            objectLock.ExitReadLock();
        }
    }

    public List<IMapObject> getMapObjects()
    {
        objectLock.EnterReadLock();
        try
        {
            return new(mapobjects.Values);
        }
        finally
        {
            objectLock.ExitReadLock();
        }
    }

    public NPC? getNPCById(int id)
    {
        foreach (IMapObject obj in getMapObjects())
        {
            if (obj.getType() == MapObjectType.NPC)
            {
                NPC npc = (NPC)obj;
                if (npc.getId() == id)
                {
                    return npc;
                }
            }
        }

        return null;
    }

    public bool containsNPC(int npcid)
    {
        objectLock.EnterReadLock();
        try
        {
            foreach (var obj in getMapObjects())
            {
                if (obj.getType() == MapObjectType.NPC)
                {
                    if (((NPC)obj).getId() == npcid)
                    {
                        return true;
                    }
                }
            }
        }
        finally
        {
            objectLock.ExitReadLock();
        }
        return false;
    }

    public void destroyNPC(int npcid)
    {     // assumption: there's at most one of the same NPC in a map.
        var npcs = getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.NPC));

        chrLock.EnterReadLock();
        objectLock.EnterWriteLock();
        try
        {
            foreach (IMapObject obj in npcs)
            {
                if (((NPC)obj).getId() == npcid)
                {
                    broadcastMessage(PacketCreator.removeNPCController(obj.getObjectId()));
                    broadcastMessage(PacketCreator.removeNPC(obj.getObjectId()));

                    this.mapobjects.Remove(obj.getObjectId());
                }
            }
        }
        finally
        {
            objectLock.ExitWriteLock();
            chrLock.ExitReadLock();
        }
    }

    public IMapObject? getMapObject(int oid)
    {
        objectLock.EnterReadLock();
        try
        {
            return mapobjects.GetValueOrDefault(oid);
        }
        finally
        {
            objectLock.ExitReadLock();
        }
    }

    /**
     * returns a monster with the given oid, if no such monster exists returns
     * null
     *
     * @param oid
     * @return
     */
    public Monster? getMonsterByOid(int oid)
    {
        IMapObject? mmo = getMapObject(oid);
        return (mmo != null && mmo.getType() == MapObjectType.MONSTER) ? (Monster)mmo : null;
    }

    public Reactor? getReactorByOid(int oid)
    {
        IMapObject? mmo = getMapObject(oid);
        return (mmo != null && mmo.getType() == MapObjectType.REACTOR) ? (Reactor)mmo : null;
    }

    public Reactor? getReactorById(int Id)
    {
        objectLock.EnterReadLock();
        try
        {
            foreach (var obj in getMapObjects())
            {
                if (obj.getType() == MapObjectType.REACTOR)
                {
                    if (((Reactor)obj).getId() == Id)
                    {
                        return (Reactor)obj;
                    }
                }
            }
            return null;
        }
        finally
        {
            objectLock.ExitReadLock();
        }
    }

    public List<Reactor> getReactorsByIdRange(int first, int last)
    {
        List<Reactor> list = new();

        objectLock.EnterReadLock();
        try
        {
            foreach (var obj in getMapObjects())
            {
                if (obj.getType() == MapObjectType.REACTOR)
                {
                    Reactor mr = (Reactor)obj;

                    if (mr.getId() >= first && mr.getId() <= last)
                    {
                        list.Add(mr);
                    }
                }
            }

            return list;
        }
        finally
        {
            objectLock.ExitReadLock();
        }
    }

    public Reactor? getReactorByName(string name)
    {
        objectLock.EnterReadLock();
        try
        {
            foreach (IMapObject obj in getMapObjects())
            {
                if (obj.getType() == MapObjectType.REACTOR)
                {
                    if (((Reactor)obj).getName().Equals(name))
                    {
                        return (Reactor)obj;
                    }
                }
            }
        }
        finally
        {
            objectLock.ExitReadLock();
        }
        return null;
    }

    public void spawnMonsterOnGroundBelow(int id, int x, int y)
    {
        var mob = LifeFactory.getMonster(id);
        spawnMonsterOnGroundBelow(mob, new Point(x, y));
    }

    public void spawnMonsterOnGroundBelow(Monster mob, Point pos)
    {
        Point spos = new Point(pos.X, pos.Y - 1);
        var calcedPos = calcPointBelow(spos);
        if (calcedPos != null)
        {
            spos = calcedPos.Value;
            spos.Y--;
            mob.setPosition(spos);
            spawnMonster(mob);
        }

    }

    public void spawnCPQMonster(Monster mob, Point pos, int team)
    {
        Point spos = new Point(pos.X, pos.Y - 1);
        var calcedPos = calcPointBelow(spos);
        if (calcedPos != null)
        {
            spos = calcedPos.Value;
            spos.Y--;
            mob.setPosition(spos);
            mob.setTeam(team);
            spawnMonster(mob);
        }

    }

    private void monsterItemDrop(Monster m, long delay)
    {
        m.dropFromFriendlyMonster(delay);
    }

    public void spawnFakeMonsterOnGroundBelow(Monster mob, Point pos)
    {
        Point spos = getGroundBelow(pos);
        mob.setPosition(spos);
        spawnFakeMonster(mob);
    }

    public Point getGroundBelow(Point pos)
    {
        Point spos = new Point(pos.X, pos.Y - 14); // Using -14 fixes spawning pets causing a lot of issues.
        var calcedPos = calcPointBelow(spos);
        if (calcedPos != null)
        {
            spos = calcedPos.Value;
        }
        spos.Y--;//shouldn't be null!
        return spos;
    }

    public Point? getPointBelow(Point pos)
    {
        return calcPointBelow(pos);
    }

    public void spawnRevives(Monster monster)
    {
        monster.setMap(this);
        if (getEventInstance() != null)
        {
            getEventInstance()!.registerMonster(monster);
        }

        spawnAndAddRangedMapObject(monster, c => c.sendPacket(PacketCreator.spawnMonster(monster, false)));

        monster.aggroUpdateController();
        updateBossSpawn(monster);

        spawnedMonstersOnMap.incrementAndGet();
        addSelfDestructive(monster);
        applyRemoveAfter(monster);
    }

    private void applyRemoveAfter(Monster monster)
    {
        var selfDestruction = monster.getStats().selfDestruction();
        if (monster.getStats().removeAfter() > 0 || selfDestruction != null && selfDestruction.getHp() < 0)
        {
            Action removeAfterAction;

            if (selfDestruction == null)
            {
                removeAfterAction = () =>
                {
                    killMonster(monster, null, false);
                };

                registerMapSchedule(removeAfterAction, monster.getStats().removeAfter() * 1000);
            }
            else
            {
                removeAfterAction = () =>
                {
                    killMonster(monster, null, false, selfDestruction.getAction());
                };

                registerMapSchedule(removeAfterAction, selfDestruction.removeAfter() * 1000);
            }

            monster.pushRemoveAfterAction(removeAfterAction);
        }
    }

    public void dismissRemoveAfter(Monster monster)
    {
        var removeAfterAction = monster.popRemoveAfterAction();
        if (removeAfterAction != null)
        {
            OverallService service = (OverallService)this.getChannelServer().getServiceAccess(ChannelServices.OVERALL);
            service.forceRunOverallAction(mapid, removeAfterAction);
        }
    }

    private List<SpawnPoint> getMonsterSpawn()
    {
        lock (monsterSpawn)
        {
            return new(monsterSpawn);
        }
    }

    private List<SpawnPoint> getAllMonsterSpawn()
    {
        lock (allMonsterSpawn)
        {
            return new(allMonsterSpawn);
        }
    }

    public void spawnAllMonsterIdFromMapSpawnList(int id)
    {
        spawnAllMonsterIdFromMapSpawnList(id, 1, false);
    }

    public void spawnAllMonsterIdFromMapSpawnList(int id, int difficulty, bool isPq)
    {
        foreach (SpawnPoint sp in getAllMonsterSpawn())
        {
            if (sp.getMonsterId() == id && sp.shouldForceSpawn())
            {
                spawnMonster(sp.getMonster(), difficulty, isPq);
            }
        }
    }

    public void spawnAllMonstersFromMapSpawnList()
    {
        spawnAllMonstersFromMapSpawnList(1, false);
    }

    public void spawnAllMonstersFromMapSpawnList(int difficulty, bool isPq)
    {
        foreach (SpawnPoint sp in getAllMonsterSpawn())
        {
            spawnMonster(sp.getMonster(), difficulty, isPq);
        }
    }

    public void spawnMonster(Monster monster)
    {
        spawnMonster(monster, 1, false);
    }

    public void spawnMonster(Monster monster, int difficulty, bool isPq)
    {
        if (mobCapacity != -1 && mobCapacity == spawnedMonstersOnMap.get())
        {
            return;//PyPQ
        }

        monster.changeDifficulty(difficulty, isPq);

        monster.setMap(this);
        if (getEventInstance() != null)
        {
            getEventInstance()!.registerMonster(monster);
        }

        spawnAndAddRangedMapObject(monster, c => c.sendPacket(PacketCreator.spawnMonster(monster, true)), null);

        monster.aggroUpdateController();
        updateBossSpawn(monster);

        if ((monster.getTeam() == 1 || monster.getTeam() == 0) && (isCPQMap() || isCPQMap2()))
        {
            List<MCSkill>? teamS = null;
            if (monster.getTeam() == 0)
            {
                teamS = redTeamBuffs;
            }
            else if (monster.getTeam() == 1)
            {
                teamS = blueTeamBuffs;
            }
            if (teamS != null)
            {
                foreach (MCSkill skil in teamS)
                {
                    if (skil != null)
                    {
                        skil.getSkill().applyEffect(null, monster, false, null);
                    }
                }
            }
        }

        if (monster.getDropPeriodTime() > 0)
        { //9300102 - Watchhog, 9300061 - Moon Bunny (HPQ), 9300093 - Tylus
            if (monster.getId() == MobId.WATCH_HOG)
            {
                monsterItemDrop(monster, monster.getDropPeriodTime());
            }
            else if (monster.getId() == MobId.MOON_BUNNY)
            {
                monsterItemDrop(monster, monster.getDropPeriodTime() / 3);
            }
            else if (monster.getId() == MobId.TYLUS)
            {
                monsterItemDrop(monster, monster.getDropPeriodTime());
            }
            else if (monster.getId() == MobId.GIANT_SNOWMAN_LV5_EASY || monster.getId() == MobId.GIANT_SNOWMAN_LV5_MEDIUM || monster.getId() == MobId.GIANT_SNOWMAN_LV5_HARD)
            {
                monsterItemDrop(monster, monster.getDropPeriodTime());
            }
            else
            {
                log.Error("UNCODED TIMED MOB DETECTED: {MonsterId}", monster.getId());
            }
        }

        spawnedMonstersOnMap.incrementAndGet();
        addSelfDestructive(monster);
        applyRemoveAfter(monster);  // thanks LightRyuzaki for pointing issues with spawned CWKPQ mobs not applying this
    }

    public void spawnDojoMonster(Monster monster)
    {
        Point[] pts = { new Point(140, 0), new Point(190, 7), new Point(187, 7) };
        spawnMonsterWithEffect(monster, 15, pts[Randomizer.nextInt(3)]);
    }

    public void spawnMonsterWithEffect(Monster monster, int effect, Point pos)
    {
        monster.setMap(this);
        Point spos = new Point(pos.X, pos.Y - 1);
        var d = calcPointBelow(spos);
        if (d == null)
        {
            return;
        }
        spos = d.Value;

        if (getEventInstance() != null)
        {
            getEventInstance()!.registerMonster(monster);
        }

        spos.Y--;
        monster.setPosition(spos);
        monster.setSpawnEffect(effect);

        spawnAndAddRangedMapObject(monster, c => c.sendPacket(PacketCreator.spawnMonster(monster, true, effect)));

        monster.aggroUpdateController();
        updateBossSpawn(monster);

        spawnedMonstersOnMap.incrementAndGet();
        addSelfDestructive(monster);
        applyRemoveAfter(monster);
    }

    public void spawnFakeMonster(Monster monster)
    {
        monster.setMap(this);
        monster.setFake(true);
        spawnAndAddRangedMapObject(monster, c => c.sendPacket(PacketCreator.spawnFakeMonster(monster, 0)));

        spawnedMonstersOnMap.incrementAndGet();
        addSelfDestructive(monster);
    }

    public void makeMonsterReal(Monster monster)
    {
        monster.setFake(false);
        broadcastMessage(PacketCreator.makeMonsterReal(monster));
        monster.aggroUpdateController();
        updateBossSpawn(monster);
    }

    public void spawnReactor(Reactor reactor)
    {
        reactor.setMap(this);
        spawnAndAddRangedMapObject(reactor, c => c.sendPacket(reactor.makeSpawnData()));
    }

    public void spawnDoor(DoorObject door)
    {
        spawnAndAddRangedMapObject(door, c =>
        {
            var chr = c.OnlinedCharacter;
            if (chr != null)
            {
                door.sendSpawnData(c, false);
                chr.addVisibleMapObject(door);
            }
        }, chr => chr.getMapId() == door.getFrom().getId());
    }

    public Portal? getDoorPortal(int doorid)
    {
        Portal? doorPortal = portals.GetValueOrDefault(0x80 + doorid);
        if (doorPortal == null)
        {
            log.Warning("[Door] {MapName} ({MapId}) does not contain door portalid {DoorId}", mapName, mapid, doorid);
            return portals.GetValueOrDefault(0x80);
        }

        return doorPortal;
    }

    public void spawnSummon(Summon summon)
    {
        spawnAndAddRangedMapObject(summon, c =>
        {
            if (summon != null)
            {
                c.sendPacket(PacketCreator.spawnSummon(summon, true));
            }
        }, null);
    }

    public void spawnMist(Mist mist, int duration, bool poison, bool fake, bool recovery)
    {
        addMapObject(mist);
        broadcastMessage(fake ? mist.makeFakeSpawnData(30) : mist.makeSpawnData());
        TimerManager tMan = TimerManager.getInstance();
        ScheduledFuture? poisonSchedule;
        if (poison)
        {
            Action poisonTask = () =>
            {
                List<IMapObject> affectedMonsters = getMapObjectsInBox(mist.getBox(), Collections.singletonList(MapObjectType.MONSTER));
                foreach (IMapObject mo in affectedMonsters)
                {
                    if (mist.makeChanceResult())
                    {
                        MonsterStatusEffect poisonEffect = new MonsterStatusEffect(Collections.singletonMap(MonsterStatus.POISON, 1), mist.getSourceSkill());
                        ((Monster)mo).applyStatus(mist.getOwner(), poisonEffect, true, duration);
                    }
                }
            };
            poisonSchedule = tMan.register(poisonTask, 2000, 2500);
        }
        else if (recovery)
        {
            Action poisonTask = () =>
            {
                List<IMapObject> players = getMapObjectsInBox(mist.getBox(), Collections.singletonList(MapObjectType.PLAYER));
                foreach (IMapObject mo in players)
                {
                    if (mist.makeChanceResult())
                    {
                        IPlayer chr = (IPlayer)mo;
                        if (mist.getOwner()?.getId() == chr.getId() || mist.getOwner()?.getParty() != null && mist.getOwner().getParty().containsMembers(chr))
                        {
                            chr.addMP(mist.getSourceSkill().getEffect(chr.getSkillLevel(mist.getSourceSkill().getId())).getX() * chr.getMp() / 100);
                        }
                    }
                }
            };
            poisonSchedule = tMan.register(poisonTask, 2000, 2500);
        }
        else
        {
            poisonSchedule = null;
        }

        Action mistSchedule = () =>
        {
            removeMapObject(mist);
            if (poisonSchedule != null)
            {
                poisonSchedule.cancel(false);
            }
            broadcastMessage(mist.makeDestroyData());
        };

        MobMistService service = (MobMistService)this.getChannelServer().getServiceAccess(ChannelServices.MOB_MIST);
        service.registerMobMistCancelAction(mapid, mistSchedule, duration);
    }

    public void spawnKite(Kite kite)
    {
        addMapObject(kite);
        broadcastMessage(kite.makeSpawnData());

        var expireKite = () =>
        {
            removeMapObject(kite);
            broadcastMessage(kite.makeDestroyData());
        };

        getWorldServer().registerTimedMapObject(expireKite, YamlConfig.config.server.KITE_EXPIRE_TIME);
    }

    public void spawnItemDrop(IMapObject dropper, IPlayer owner, Item item, Point pos, bool ffaDrop, bool playerDrop)
    {
        spawnItemDrop(dropper, owner, item, pos, (byte)(ffaDrop ? 2 : 0), playerDrop);
    }

    public void spawnItemDrop(IMapObject dropper, IPlayer owner, Item item, Point pos, byte dropType, bool playerDrop)
    {
        if (FieldLimit.DROP_LIMIT.check(this.getFieldLimit()))
        {
            // thanks Conrad for noticing some maps shouldn't have loots available
            this.disappearingItemDrop(dropper, owner, item, pos);
            return;
        }

        Point droppos = calcDropPos(pos, pos);
        MapItem mdrop = new MapItem(item, droppos, dropper, owner, dropType, playerDrop);
        mdrop.setDropTime(Server.getInstance().getCurrentTime());

        spawnAndAddRangedMapObject(mdrop, c =>
        {
            mdrop.lockItem();
            try
            {
                c.sendPacket(PacketCreator.dropItemFromMapObject(c.OnlinedCharacter, mdrop, dropper.getPosition(), droppos, 1));
            }
            finally
            {
                mdrop.unlockItem();
            }
        }, null);

        mdrop.lockItem();
        try
        {
            broadcastItemDropMessage(mdrop, dropper.getPosition(), droppos, 0);
        }
        finally
        {
            mdrop.unlockItem();
        }

        instantiateItemDrop(mdrop);
        activateItemReactors(mdrop, owner.getClient());
    }

    public void spawnItemDropList(List<int> list, IMapObject dropper, IPlayer owner, Point pos)
    {
        spawnItemDropList(list, 1, 1, dropper, owner, pos, true, false);
    }

    public void spawnItemDropList(List<int> list, int minCopies, int maxCopies, IMapObject dropper, IPlayer owner, Point pos)
    {
        spawnItemDropList(list, minCopies, maxCopies, dropper, owner, pos, true, false);
    }

    // spawns item instances of all defined item ids on a list
    public void spawnItemDropList(List<int> list, int minCopies, int maxCopies, IMapObject dropper, IPlayer owner, Point pos, bool ffaDrop, bool playerDrop)
    {
        int copies = (maxCopies - minCopies) + 1;
        if (copies < 1)
        {
            return;
        }

        Collections.shuffle(list);

        ItemInformationProvider ii = ItemInformationProvider.getInstance();
        Random rnd = new Random();

        Point dropPos = pos;
        dropPos.X -= (12 * list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == 0)
            {
                spawnMesoDrop(owner != null ? 10 * owner.getMesoRate() : 10, calcDropPos(dropPos, pos), dropper, owner, playerDrop, (byte)(ffaDrop ? 2 : 0));
            }
            else
            {
                Item drop;
                int randomedId = list[i];

                if (ItemConstants.getInventoryType(randomedId) != InventoryType.EQUIP)
                {
                    drop = new Item(randomedId, 0, (short)(rnd.Next(copies) + minCopies));
                }
                else
                {
                    drop = ii.randomizeStats((Equip)ii.getEquipById(randomedId));
                }

                spawnItemDrop(dropper, owner, drop, calcDropPos(dropPos, pos), ffaDrop, playerDrop);
            }

            dropPos.X += 25;
        }
    }

    private void registerMapSchedule(AbstractRunnable r, long delay)
    {
        OverallService service = (OverallService)this.getChannelServer().getServiceAccess(ChannelServices.OVERALL);
        service.registerOverallAction(mapid, r, delay);
    }

    private void registerMapSchedule(Action r, long delay)
    {
        OverallService service = (OverallService)this.getChannelServer().getServiceAccess(ChannelServices.OVERALL);
        service.registerOverallAction(mapid, r, delay);
    }

    private void activateItemReactors(MapItem drop, IClient c)
    {
        Item item = drop.getItem();

        foreach (IMapObject o in getReactors())
        {
            Reactor react = (Reactor)o;

            if (react.getReactorType() == 100)
            {
                var reactItem = react.getReactItem(react.getEventState()).GetValueOrDefault();
                if (reactItem.Key == item.getItemId() && reactItem.Value == item.getQuantity())
                {

                    if (react.getArea().Contains(drop.getPosition()))
                    {
                        registerMapSchedule(new ActivateItemReactor(this, drop, react, c), 5000);
                        break;
                    }
                }
            }
        }
    }

    public void searchItemReactors(Reactor react)
    {
        if (react.getReactorType() == 100)
        {
            var reactProp = react.getReactItem(react.getEventState()).GetValueOrDefault();
            int reactItem = reactProp.Key, reactQty = reactProp.Value;
            Rectangle reactArea = react.getArea();

            List<MapItem> list;
            objectLock.EnterReadLock();
            try
            {
                list = new(droppedItems.Keys);
            }
            finally
            {
                objectLock.ExitReadLock();
            }

            foreach (MapItem drop in list)
            {
                drop.lockItem();
                try
                {
                    if (!drop.isPickedUp())
                    {
                        Item item = drop.getItem();

                        if (item != null && reactItem == item.getItemId() && reactQty == item.getQuantity())
                        {
                            if (reactArea.Contains(drop.getPosition()))
                            {
                                var owner = drop.getOwnerClient();
                                if (owner != null)
                                {
                                    registerMapSchedule(new ActivateItemReactor(this, drop, react, owner), 5000);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    drop.unlockItem();
                }
            }
        }
    }

    public void changeEnvironment(string mapObj, int newState)
    {
        broadcastMessage(PacketCreator.environmentChange(mapObj, newState));
    }

    public void startMapEffect(string msg, int itemId)
    {
        startMapEffect(msg, itemId, 30000);
    }

    public void startMapEffect(string msg, int itemId, long time)
    {
        if (mapEffect != null)
        {
            return;
        }
        mapEffect = new MapEffect(msg, itemId);
        broadcastMessage(mapEffect.makeStartData());

        Action r = () =>
        {
            broadcastMessage(mapEffect.makeDestroyData());
            mapEffect = null;
        };

        registerMapSchedule(r, time);
    }

    public IPlayer? getAnyCharacterFromParty(int partyid)
    {
        foreach (IPlayer chr in this.getAllPlayers())
        {
            if (chr.getPartyId() == partyid)
            {
                return chr;
            }
        }

        return null;
    }

    private void addPartyMemberInternal(IPlayer chr, int partyid)
    {
        if (partyid == -1)
        {
            return;
        }

        HashSet<int>? partyEntry = mapParty.GetValueOrDefault(partyid);
        if (partyEntry == null)
        {
            partyEntry = new();
            partyEntry.Add(chr.getId());

            mapParty.AddOrUpdate(partyid, partyEntry);
        }
        else
        {
            partyEntry.Add(chr.getId());
        }
    }

    private void removePartyMemberInternal(IPlayer chr, int partyid)
    {
        if (partyid == -1)
        {
            return;
        }

        HashSet<int>? partyEntry = mapParty.GetValueOrDefault(partyid);
        if (partyEntry != null)
        {
            if (partyEntry.Count > 1)
            {
                partyEntry.Remove(chr.getId());
            }
            else
            {
                mapParty.Remove(partyid);
            }
        }
    }

    public void addPartyMember(IPlayer chr, int partyid)
    {
        chrLock.EnterWriteLock();
        try
        {
            addPartyMemberInternal(chr, partyid);
        }
        finally
        {
            chrLock.ExitWriteLock();
        }
    }

    public void removePartyMember(IPlayer chr, int partyid)
    {
        chrLock.EnterWriteLock();
        try
        {
            removePartyMemberInternal(chr, partyid);
        }
        finally
        {
            chrLock.ExitWriteLock();
        }
    }

    public void removeParty(int partyid)
    {
        chrLock.EnterWriteLock();
        try
        {
            mapParty.Remove(partyid);
        }
        finally
        {
            chrLock.ExitWriteLock();
        }
    }

    public void addPlayer(IPlayer chr)
    {
        int chrSize;
        var party = chr.getParty();
        chrLock.EnterWriteLock();
        try
        {
            characters.Add(chr);
            chrSize = characters.Count;

            if (party != null && party.getMemberById(chr.getId()) != null)
            {
                addPartyMemberInternal(chr, party.getId());
            }
            itemMonitorTimeout = 1;
        }
        finally
        {
            chrLock.ExitWriteLock();
        }

        chr.setMapId(mapid);
        chr.updateActiveEffects();

        if (this.getHPDec() > 0)
        {
            getWorldServer().addPlayerHpDecrease(chr);
        }
        else
        {
            getWorldServer().removePlayerHpDecrease(chr);
        }

        MapScriptManager msm = MapScriptManager.getInstance();
        if (chrSize == 1)
        {
            if (!hasItemMonitor())
            {
                startItemMonitor();
                aggroMonitor.startAggroCoordinator();
            }

            if (onFirstUserEnter.Length != 0)
            {
                msm.runMapScript(chr.getClient(), "onFirstUserEnter/" + onFirstUserEnter, true);
            }
        }
        if (onUserEnter.Length != 0)
        {
            if (onUserEnter.Equals("cygnusTest") && !MapId.isCygnusIntro(mapid))
            {
                chr.saveLocation("INTRO");
            }

            msm.runMapScript(chr.getClient(), "onUserEnter/" + onUserEnter, false);
        }
        if (FieldLimit.CANNOTUSEMOUNTS.check(fieldLimit) && chr.getBuffedValue(BuffStat.MONSTER_RIDING) != null)
        {
            chr.cancelEffectFromBuffStat(BuffStat.MONSTER_RIDING);
            chr.cancelBuffStats(BuffStat.MONSTER_RIDING);
        }

        if (mapid == MapId.FROM_LITH_TO_RIEN)
        { // To Rien
            int travelTime = getWorldServer().getTransportationTime((int)TimeSpan.FromMinutes(1).TotalMilliseconds);
            chr.sendPacket(PacketCreator.getClock(travelTime / 1000));
            TimerManager.getInstance().schedule(() =>
            {
                if (chr.getMapId() == MapId.FROM_LITH_TO_RIEN)
                {
                    chr.changeMap(MapId.DANGEROUS_FOREST, 0);
                }
            }, travelTime);
        }
        else if (mapid == MapId.FROM_RIEN_TO_LITH)
        { // To Lith Harbor
            int travelTime = getWorldServer().getTransportationTime((int)TimeSpan.FromMinutes(1).TotalMilliseconds);
            chr.sendPacket(PacketCreator.getClock(travelTime / 1000));
            TimerManager.getInstance().schedule(() =>
            {
                if (chr.getMapId() == MapId.FROM_RIEN_TO_LITH)
                {
                    chr.changeMap(MapId.LITH_HARBOUR, 3);
                }
            }, travelTime);
        }
        else if (mapid == MapId.FROM_ELLINIA_TO_EREVE)
        { // To Ereve (SkyFerry)
            int travelTime = getWorldServer().getTransportationTime((int)TimeSpan.FromMinutes(2).TotalMilliseconds);
            chr.sendPacket(PacketCreator.getClock(travelTime / 1000));
            TimerManager.getInstance().schedule(() =>
            {
                if (chr.getMapId() == MapId.FROM_ELLINIA_TO_EREVE)
                {
                    chr.changeMap(MapId.SKY_FERRY, 0);
                }
            }, travelTime);
        }
        else if (mapid == MapId.FROM_EREVE_TO_ELLINIA)
        { // To Victoria Island (SkyFerry)
            int travelTime = getWorldServer().getTransportationTime((int)TimeSpan.FromMinutes(2).TotalMilliseconds);
            chr.sendPacket(PacketCreator.getClock(travelTime / 1000));
            TimerManager.getInstance().schedule(() =>
            {
                if (chr.getMapId() == MapId.FROM_EREVE_TO_ELLINIA)
                {
                    chr.changeMap(MapId.ELLINIA_SKY_FERRY, 0);
                }
            }, travelTime);
        }
        else if (mapid == MapId.FROM_EREVE_TO_ORBIS)
        { // To Orbis (SkyFerry)
            int travelTime = getWorldServer().getTransportationTime((int)TimeSpan.FromMinutes(8).TotalMilliseconds);
            chr.sendPacket(PacketCreator.getClock(travelTime / 1000));
            TimerManager.getInstance().schedule(() =>
            {
                if (chr.getMapId() == MapId.FROM_EREVE_TO_ORBIS)
                {
                    chr.changeMap(MapId.ORBIS_STATION, 0);
                }
            }, travelTime);
        }
        else if (mapid == MapId.FROM_ORBIS_TO_EREVE)
        { // To Ereve From Orbis (SkyFerry)
            int travelTime = getWorldServer().getTransportationTime((int)TimeSpan.FromMinutes(8).TotalMilliseconds);
            chr.sendPacket(PacketCreator.getClock(travelTime / 1000));
            TimerManager.getInstance().schedule(() =>
            {
                if (chr.getMapId() == MapId.FROM_ORBIS_TO_EREVE)
                {
                    chr.changeMap(MapId.SKY_FERRY, 0);
                }
            }, travelTime);
        }
        else if (MiniDungeonInfo.isDungeonMap(mapid))
        {
            var mmd = chr.getClient().getChannelServer().getMiniDungeon(mapid);
            if (mmd != null)
            {
                mmd.registerPlayer(chr);
            }
        }
        else if (GameConstants.isAriantColiseumArena(mapid))
        {
            int pqTimer = (int)TimeSpan.FromMinutes(10).TotalMilliseconds;
            chr.sendPacket(PacketCreator.getClock(pqTimer / 1000));
        }

        Pet?[] pets = chr.getPets();
        foreach (Pet? pet in pets)
        {
            if (pet != null)
            {
                pet.setPos(getGroundBelow(chr.getPosition()));
                chr.sendPacket(PacketCreator.showPet(chr, pet, false, false));
            }
            else
            {
                break;
            }
        }
        chr.commitExcludedItems();  // thanks OishiiKawaiiDesu for noticing pet item ignore registry erasing upon changing maps

        if (chr.getMonsterCarnival() != null)
        {
            chr.sendPacket(PacketCreator.getClock(chr.getMonsterCarnival()!.getTimeLeftSeconds()));
            if (isCPQMap())
            {
                int team = -1;
                int oposition = -1;
                if (chr.getTeam() == 0)
                {
                    team = 0;
                    oposition = 1;
                }
                if (chr.getTeam() == 1)
                {
                    team = 1;
                    oposition = 0;
                }
                chr.sendPacket(PacketCreator.startMonsterCarnival(chr, team, oposition));
            }
        }

        chr.removeSandboxItems();

        if (chr.getChalkboard() != null)
        {
            if (!GameConstants.isFreeMarketRoom(mapid))
            {
                chr.sendPacket(PacketCreator.useChalkboard(chr, false)); // update player's chalkboard when changing maps found thanks to Vcoc
            }
            else
            {
                chr.setChalkboard(null);
            }
        }

        if (chr.isHidden())
        {
            broadcastGMSpawnPlayerMapObjectMessage(chr, chr, true);
            chr.sendPacket(PacketCreator.getGMEffect(0x10, 1));

            broadcastGMMessage(chr, PacketCreator.giveForeignBuff(chr.getId(), new BuffStatValue(BuffStat.DARKSIGHT, 0)), false);
        }
        else
        {
            broadcastSpawnPlayerMapObjectMessage(chr, chr, true);
        }

        sendObjectPlacement(chr.getClient());

        if (isStartingEventMap() && !eventStarted())
        {
            chr.getMap().getPortal("join00").setPortalStatus(false);
        }
        if (hasForcedEquip())
        {
            chr.sendPacket(PacketCreator.showForcedEquip(-1));
        }
        if (specialEquip())
        {
            chr.sendPacket(PacketCreator.coconutScore(0, 0));
            chr.sendPacket(PacketCreator.showForcedEquip(chr.getTeam()));
        }
        objectLock.EnterWriteLock();
        try
        {
            this.mapobjects.AddOrUpdate(chr.getObjectId(), chr);
        }
        finally
        {
            objectLock.ExitWriteLock();
        }

        if (chr.getPlayerShop() != null)
        {
            addMapObject(chr.getPlayerShop()!);
        }

        var dragon = chr.getDragon();
        if (dragon != null)
        {
            dragon.setPosition(chr.getPosition());
            this.addMapObject(dragon);
            if (chr.isHidden())
            {
                this.broadcastGMPacket(chr, PacketCreator.spawnDragon(dragon));
            }
            else
            {
                this.broadcastPacket(chr, PacketCreator.spawnDragon(dragon));
            }
        }

        StatEffect? summonStat = chr.getStatForBuff(BuffStat.SUMMON);
        if (summonStat != null)
        {
            Summon? summon = chr.getSummonByKey(summonStat.getSourceId());
            summon.setPosition(chr.getPosition());
            chr.getMap().spawnSummon(summon);
            updateMapObjectVisibility(chr, summon);
        }
        if (mapEffect != null)
        {
            mapEffect.sendStartData(chr.getClient());
        }
        chr.sendPacket(PacketCreator.resetForcedStats());
        if (MapId.isGodlyStatMap(mapid))
        {
            chr.sendPacket(PacketCreator.aranGodlyStats());
        }
        if (chr.getEventInstance() != null && chr.getEventInstance()!.isTimerStarted())
        {
            chr.sendPacket(PacketCreator.getClock((int)(chr.getEventInstance()!.getTimeLeft() / 1000)));
        }
        if (chr.getFitness() != null && chr.getFitness()!.isTimerStarted())
        {
            chr.sendPacket(PacketCreator.getClock((int)(chr.getFitness()!.getTimeLeft() / 1000)));
        }

        if (chr.getOla() != null && chr.getOla()!.isTimerStarted())
        {
            chr.sendPacket(PacketCreator.getClock((int)(chr.getOla()!.getTimeLeft() / 1000)));
        }

        if (mapid == MapId.EVENT_SNOWBALL)
        {
            chr.sendPacket(PacketCreator.rollSnowBall());
        }

        if (hasClock())
        {
            DateTimeOffset cal = DateTimeOffset.Now;
            chr.sendPacket(PacketCreator.getClockTime(cal.Hour, cal.Minute, cal.Second));
        }
        if (hasBoat() > 0)
        {
            if (hasBoat() == 1)
            {
                chr.sendPacket((PacketCreator.boatPacket(true)));
            }
            else
            {
                chr.sendPacket(PacketCreator.boatPacket(false));
            }
        }

        chr.receivePartyMemberHP();
        announcePlayerDiseases(chr.getClient());
    }

    private static void announcePlayerDiseases(IClient c)
    {
        Server.getInstance().registerAnnouncePlayerDiseases(c);
    }

    public Portal getRandomPlayerSpawnpoint()
    {
        List<Portal> spawnPoints = new();
        foreach (Portal localPortal in portals.Values)
        {
            if (localPortal.getType() >= 0 && localPortal.getType() <= 1 && localPortal.getTargetMapId() == MapId.NONE)
            {
                spawnPoints.Add(localPortal);
            }
        }
        var portal = spawnPoints.get(Randomizer.nextInt(spawnPoints.Count));
        return portal ?? getPortal(0)!;
    }

    public Portal? findClosestTeleportPortal(Point from)
    {
        Portal? closest = null;
        double shortestDistance = double.PositiveInfinity;
        foreach (Portal portal in portals.Values)
        {
            double distance = portal.getPosition().distanceSq(from);
            if (portal.getType() == PortalConstants.TELEPORT_PORTAL && distance < shortestDistance && portal.getTargetMapId() != MapId.NONE)
            {
                closest = portal;
                shortestDistance = distance;
            }
        }
        return closest;
    }

    public Portal? findClosestPlayerSpawnpoint(Point from)
    {
        Portal? closest = null;
        double shortestDistance = double.PositiveInfinity;
        foreach (Portal portal in portals.Values)
        {
            double distance = portal.getPosition().distanceSq(from);
            if (portal.getType() >= 0 && portal.getType() <= 1 && distance < shortestDistance && portal.getTargetMapId() == MapId.NONE)
            {
                closest = portal;
                shortestDistance = distance;
            }
        }
        return closest;
    }

    public Portal? findClosestPortal(Point from)
    {
        Portal? closest = null;
        double shortestDistance = double.PositiveInfinity;
        foreach (Portal portal in portals.Values)
        {
            double distance = portal.getPosition().distanceSq(from);
            if (distance < shortestDistance)
            {
                closest = portal;
                shortestDistance = distance;
            }
        }
        return closest;
    }

    public Portal? findMarketPortal()
    {
        foreach (Portal portal in portals.Values)
        {
            string ptScript = portal.getScriptName();
            if (ptScript != null && ptScript.Contains("market"))
            {
                return portal;
            }
        }
        return null;
    }

    /*
    public Collection<Portal> getPortals() {
        return Collections.unmodifiableCollection(portals.values());
    }
    */

    public void addPlayerPuppet(IPlayer player)
    {
        foreach (Monster mm in this.getAllMonsters())
        {
            mm.aggroAddPuppet(player);
        }
    }

    public void removePlayerPuppet(IPlayer player)
    {
        foreach (Monster mm in this.getAllMonsters())
        {
            mm.aggroRemovePuppet(player);
        }
    }

    public void removePlayer(IPlayer chr)
    {
        var cserv = chr.getClient().getChannelServer();
        chr.unregisterChairBuff();

        var party = chr.getParty();
        chrLock.EnterWriteLock();
        try
        {
            if (party != null && party.getMemberById(chr.getId()) != null)
            {
                removePartyMemberInternal(chr, party.getId());
            }

            characters.Remove(chr);
        }
        finally
        {
            chrLock.ExitWriteLock();
        }

        if (MiniDungeonInfo.isDungeonMap(mapid))
        {
            var mmd = cserv.getMiniDungeon(mapid);
            if (mmd != null)
            {
                if (!mmd.unregisterPlayer(chr))
                {
                    cserv.removeMiniDungeon(mapid);
                }
            }
        }

        removeMapObject(chr.getObjectId());
        if (!chr.isHidden())
        {
            broadcastMessage(PacketCreator.removePlayerFromMap(chr.getId()));
        }
        else
        {
            broadcastGMMessage(PacketCreator.removePlayerFromMap(chr.getId()));
        }

        chr.leaveMap();

        foreach (Summon summon in chr.getSummonsValues())
        {
            if (summon.isStationary())
            {
                chr.cancelEffectFromBuffStat(BuffStat.PUPPET);
            }
            else
            {
                removeMapObject(summon);
            }
        }

        if (chr.getDragon() != null)
        {
            removeMapObject(chr.getDragon()!);
            if (chr.isHidden())
            {
                this.broadcastGMPacket(chr, PacketCreator.removeDragon(chr.getId()));
            }
            else
            {
                this.broadcastPacket(chr, PacketCreator.removeDragon(chr.getId()));
            }
        }
    }

    public void broadcastMessage(Packet packet)
    {
        broadcastMessage(null, packet, double.PositiveInfinity, null);
    }

    public void broadcastGMMessage(Packet packet)
    {
        broadcastGMMessage(null, packet, double.PositiveInfinity, null);
    }

    /**
     * Nonranged. Repeat to source according to parameter.
     *
     * @param source
     * @param packet
     * @param repeatToSource
     */
    public void broadcastMessage(IPlayer? source, Packet packet, bool repeatToSource)
    {
        broadcastMessage(repeatToSource ? null : source, packet, double.PositiveInfinity, source?.getPosition());
    }

    /**
     * Ranged and repeat according to parameters.
     *
     * @param source
     * @param packet
     * @param repeatToSource
     * @param ranged
     */
    public void broadcastMessage(IPlayer? source, Packet packet, bool repeatToSource, bool ranged)
    {
        broadcastMessage(repeatToSource ? null : source, packet, ranged ? getRangedDistance() : double.PositiveInfinity, source.getPosition());
    }

    /**
     * Always ranged from Point.
     *
     * @param packet
     * @param rangedFrom
     */
    public void broadcastMessage(Packet packet, Point rangedFrom)
    {
        broadcastMessage(null, packet, getRangedDistance(), rangedFrom);
    }

    /**
     * Always ranged from point. Does not repeat to source.
     *
     * @param source
     * @param packet
     * @param rangedFrom
     */
    public void broadcastMessage(IPlayer? source, Packet packet, Point rangedFrom)
    {
        broadcastMessage(source, packet, getRangedDistance(), rangedFrom);
    }

    private void broadcastMessage(IPlayer? source, Packet packet, double rangeSq, Point? rangedFrom)
    {
        chrLock.EnterReadLock();
        try
        {
            foreach (IPlayer chr in characters)
            {
                if (chr != source)
                {
                    if (rangeSq < double.PositiveInfinity)
                    {
                        if (rangedFrom != null && rangedFrom.Value.distanceSq(chr.getPosition()) <= rangeSq)
                        {
                            chr.sendPacket(packet);
                        }
                    }
                    else
                    {
                        chr.sendPacket(packet);
                    }
                }
            }
        }
        finally
        {
            chrLock.ExitReadLock();
        }
    }

    private void updateBossSpawn(Monster monster)
    {
        if (monster.hasBossHPBar())
        {
            broadcastBossHpMessage(monster, monster.GetHashCode(), monster.makeBossHPBarPacket(), monster.getPosition());
        }
        if (monster.isBoss())
        {
            if (unclaimOwnership() != null)
            {
                string mobName = MonsterInformationProvider.getInstance().getMobNameFromId(monster.getId());
                if (mobName != null)
                {
                    mobName = mobName.Trim();
                    this.dropMessage(5, "This lawn has been taken siege by " + mobName + "'s forces and will be kept hold until their defeat.");
                }
            }
        }
    }

    public void broadcastBossHpMessage(Monster mm, int bossHash, Packet packet)
    {
        broadcastBossHpMessage(mm, bossHash, null, packet, double.PositiveInfinity, null);
    }

    public void broadcastBossHpMessage(Monster mm, int bossHash, Packet packet, Point rangedFrom)
    {
        broadcastBossHpMessage(mm, bossHash, null, packet, getRangedDistance(), rangedFrom);
    }

    private void broadcastBossHpMessage(Monster mm, int bossHash, IPlayer? source, Packet packet, double rangeSq, Point? rangedFrom)
    {
        chrLock.EnterReadLock();
        try
        {
            foreach (IPlayer chr in characters)
            {
                if (chr != source)
                {
                    if (rangeSq < double.PositiveInfinity)
                    {
                        if (rangedFrom != null && rangedFrom.Value.distanceSq(chr.getPosition()) <= rangeSq)
                        {
                            chr.getClient().announceBossHpBar(mm, bossHash, packet);
                        }
                    }
                    else
                    {
                        chr.getClient().announceBossHpBar(mm, bossHash, packet);
                    }
                }
            }
        }
        finally
        {
            chrLock.ExitReadLock();
        }
    }

    private void broadcastItemDropMessage(MapItem mdrop, Point dropperPos, Point dropPos, byte mod, Point? rangedFrom)
    {
        broadcastItemDropMessage(mdrop, dropperPos, dropPos, mod, getRangedDistance(), rangedFrom);
    }

    private void broadcastItemDropMessage(MapItem mdrop, Point dropperPos, Point dropPos, byte mod)
    {
        broadcastItemDropMessage(mdrop, dropperPos, dropPos, mod, double.PositiveInfinity, null);
    }

    private void broadcastItemDropMessage(MapItem mdrop, Point dropperPos, Point dropPos, byte mod, double rangeSq, Point? rangedFrom)
    {
        chrLock.EnterReadLock();
        try
        {
            foreach (IPlayer chr in characters)
            {
                Packet packet = PacketCreator.dropItemFromMapObject(chr, mdrop, dropperPos, dropPos, mod);

                if (rangeSq < double.PositiveInfinity)
                {
                    if (rangedFrom != null && rangedFrom.Value.distanceSq(chr.getPosition()) <= rangeSq)
                    {
                        chr.sendPacket(packet);
                    }
                }
                else
                {
                    chr.sendPacket(packet);
                }
            }
        }
        finally
        {
            chrLock.ExitReadLock();
        }
    }

    public void broadcastSpawnPlayerMapObjectMessage(IPlayer source, IPlayer player, bool enteringField)
    {
        broadcastSpawnPlayerMapObjectMessage(source, player, enteringField, false);
    }

    public void broadcastGMSpawnPlayerMapObjectMessage(IPlayer source, IPlayer player, bool enteringField)
    {
        broadcastSpawnPlayerMapObjectMessage(source, player, enteringField, true);
    }

    private void broadcastSpawnPlayerMapObjectMessage(IPlayer source, IPlayer player, bool enteringField, bool gmBroadcast)
    {
        chrLock.EnterReadLock();
        try
        {
            if (gmBroadcast)
            {
                foreach (IPlayer chr in characters)
                {
                    if (chr.isGM())
                    {
                        if (chr != source)
                        {
                            chr.sendPacket(PacketCreator.spawnPlayerMapObject(chr.getClient(), player, enteringField));
                        }
                    }
                }
            }
            else
            {
                foreach (IPlayer chr in characters)
                {
                    if (chr != source)
                    {
                        chr.sendPacket(PacketCreator.spawnPlayerMapObject(chr.getClient(), player, enteringField));
                    }
                }
            }
        }
        finally
        {
            chrLock.ExitReadLock();
        }
    }

    public void broadcastUpdateCharLookMessage(IPlayer source, IPlayer player)
    {
        chrLock.EnterReadLock();
        try
        {
            foreach (IPlayer chr in characters)
            {
                if (chr != source)
                {
                    chr.sendPacket(PacketCreator.updateCharLook(chr.getClient(), player));
                }
            }
        }
        finally
        {
            chrLock.ExitReadLock();
        }
    }

    public void dropMessage(int type, string message)
    {
        broadcastStringMessage(type, message);
    }

    public void broadcastStringMessage(int type, string message)
    {
        broadcastMessage(PacketCreator.serverNotice(type, message));
    }

    private static bool isNonRangedType(MapObjectType type)
    {
        switch (type)
        {
            case MapObjectType.NPC:
            case MapObjectType.PLAYER:
            case MapObjectType.HIRED_MERCHANT:
            case MapObjectType.PLAYER_NPC:
            case MapObjectType.DRAGON:
            case MapObjectType.MIST:
            case MapObjectType.KITE:
                return true;
            default:
                return false;
        }
    }

    private void sendObjectPlacement(IClient c)
    {
        var chr = c.OnlinedCharacter;

        foreach (var o in getMapObjects())
        {
            if (isNonRangedType(o.getType()))
            {
                o.sendSpawnData(c);
            }
            else if (o.getType() == MapObjectType.SUMMON)
            {
                Summon summon = (Summon)o;
                if (summon.getOwner() == chr)
                {
                    if (chr.isSummonsEmpty() || !chr.containsSummon(summon))
                    {
                        objectLock.EnterWriteLock();
                        try
                        {
                            mapobjects.Remove(o.getObjectId());
                        }
                        finally
                        {
                            objectLock.ExitWriteLock();
                        }

                        //continue;
                    }
                }
            }
        }

        if (chr != null)
        {
            foreach (var o in getMapObjectsInRange(chr.getPosition(), getRangedDistance(), rangedMapobjectTypes))
            {
                if (o.getType() == MapObjectType.REACTOR)
                {
                    if (((Reactor)o).isAlive())
                    {
                        o.sendSpawnData(chr.getClient());
                        chr.addVisibleMapObject(o);
                    }
                }
                else
                {
                    o.sendSpawnData(chr.getClient());
                    chr.addVisibleMapObject(o);

                    if (o.getType() == MapObjectType.MONSTER)
                    {
                        ((Monster)o).aggroUpdateController();
                    }
                }
            }
        }
    }

    public List<IMapObject> getMapObjectsInRange(Point from, double rangeSq, List<MapObjectType> types)
    {
        List<IMapObject> ret = new();
        objectLock.EnterReadLock();
        try
        {
            foreach (IMapObject l in getMapObjects())
            {
                if (types.Contains(l.getType()))
                {
                    if (from.distanceSq(l.getPosition()) <= rangeSq)
                    {
                        ret.Add(l);
                    }
                }
            }
            return ret;
        }
        finally
        {
            objectLock.ExitReadLock();
        }
    }

    public List<IMapObject> getMapObjectsInBox(Rectangle box, List<MapObjectType> types)
    {
        List<IMapObject> ret = new();
        objectLock.EnterReadLock();
        try
        {
            foreach (IMapObject l in getMapObjects())
            {
                if (types.Contains(l.getType()))
                {
                    if (box.Contains(l.getPosition()))
                    {
                        ret.Add(l);
                    }
                }
            }
            return ret;
        }
        finally
        {
            objectLock.ExitReadLock();
        }
    }

    public void addPortal(Portal myPortal)
    {
        portals.AddOrUpdate(myPortal.getId(), myPortal);
    }

    public Portal? getPortal(string portalname)
    {
        return portals.Values.FirstOrDefault(x => x.getName() == portalname);
    }

    public Portal? getPortal(int portalid)
    {
        return portals.GetValueOrDefault(portalid);
    }

    public void addMapleArea(Rectangle rec)
    {
        areas.Add(rec);
    }

    public List<Rectangle> getAreas()
    {
        return new(areas);
    }

    public Rectangle getArea(int index)
    {
        return areas.get(index);
    }

    public void setFootholds(FootholdTree footholds)
    {
        this.footholds = footholds;
    }

    public FootholdTree? getFootholds()
    {
        return footholds;
    }

    public void setMapPointBoundings(int px, int py, int h, int w)
    {
        mapArea.setBounds(px, py, w, h);
    }

    public void setMapLineBoundings(int vrTop, int vrBottom, int vrLeft, int vrRight)
    {
        mapArea.setBounds(vrLeft, vrTop, vrRight - vrLeft, vrBottom - vrTop);
    }

    public MonsterAggroCoordinator? getAggroCoordinator()
    {
        return aggroMonitor;
    }

    /**
     * it's threadsafe, gtfo :D
     *
     * @param monster
     * @param mobTime
     */
    public void addMonsterSpawn(Monster monster, int mobTime, int team)
    {
        Point newpos = calcPointBelow(monster.getPosition())!.Value;
        newpos.Y -= 1;
        SpawnPoint sp = new SpawnPoint(monster, newpos, !monster.isMobile(), mobTime, mobInterval, team);
        monsterSpawn.Add(sp);
        if (sp.shouldSpawn() || mobTime == -1)
        {
            // -1 does not respawn and should not either but force ONE spawn
            spawnMonster(sp.getMonster());
        }
    }

    public void addAllMonsterSpawn(Monster monster, int mobTime, int team)
    {
        Point newpos = calcPointBelow(monster.getPosition())!.Value;
        newpos.Y -= 1;
        SpawnPoint sp = new SpawnPoint(monster, newpos, !monster.isMobile(), mobTime, mobInterval, team);
        allMonsterSpawn.Add(sp);
    }

    public void removeMonsterSpawn(int mobId, int x, int y)
    {
        // assumption: spawn points identifies by tuple (lifeid, x, y)

        Point checkpos = calcPointBelow(new Point(x, y))!.Value;
        checkpos.Y -= 1;

        List<SpawnPoint> toRemove = new();
        foreach (SpawnPoint sp in getMonsterSpawn())
        {
            Point pos = sp.getPosition();
            if (sp.getMonsterId() == mobId && checkpos.Equals(pos))
            {
                toRemove.Add(sp);
            }
        }

        if (toRemove.Count > 0)
        {
            lock (monsterSpawn)
            {
                foreach (SpawnPoint sp in toRemove)
                {
                    monsterSpawn.Remove(sp);
                }
            }
        }
    }

    public void removeAllMonsterSpawn(int mobId, int x, int y)
    {
        // assumption: spawn points identifies by tuple (lifeid, x, y)

        Point checkpos = calcPointBelow(new Point(x, y))!.Value;
        checkpos.Y -= 1;

        List<SpawnPoint> toRemove = new();
        foreach (SpawnPoint sp in getAllMonsterSpawn())
        {
            Point pos = sp.getPosition();
            if (sp.getMonsterId() == mobId && checkpos.Equals(pos))
            {
                toRemove.Add(sp);
            }
        }

        if (toRemove.Count > 0)
        {
            lock (allMonsterSpawn)
            {
                foreach (SpawnPoint sp in toRemove)
                {
                    allMonsterSpawn.Remove(sp);
                }
            }
        }
    }

    public void reportMonsterSpawnPoints(IPlayer chr)
    {
        chr.dropMessage(6, "Mob spawnpoints on map " + getId() + ", with available Mob SPs " + monsterSpawn.Count() + ", used " + spawnedMonstersOnMap.get() + ":");
        foreach (SpawnPoint sp in getAllMonsterSpawn())
        {
            chr.dropMessage(6, "  id: " + sp.getMonsterId() + " canSpawn: " + !sp.getDenySpawn() + " numSpawned: " + sp.getSpawned() + " x: " + sp.getPosition().X + " y: " + sp.getPosition().Y + " time: " + sp.getMobTime() + " team: " + sp.getTeam());
        }
    }

    public Dictionary<int, IPlayer> getMapPlayers()
    {
        chrLock.EnterReadLock();
        try
        {
            return characters.ToDictionary(x => x.getId());
        }
        finally
        {
            chrLock.ExitReadLock();
        }
    }

    public IReadOnlyCollection<IPlayer> getCharacters()
    {
        chrLock.EnterReadLock();
        try
        {
            return new ReadOnlyCollection<IPlayer>(characters.ToArray());
        }
        finally
        {
            chrLock.ExitReadLock();
        }
    }

    public IPlayer? getCharacterById(int id)
    {
        chrLock.EnterReadLock();
        try
        {
            return characters.FirstOrDefault(x => x.getId() == id);
        }
        finally
        {
            chrLock.ExitReadLock();
        }
    }

    private static void updateMapObjectVisibility(IPlayer chr, IMapObject mo)
    {
        if (!chr.isMapObjectVisible(mo))
        { // object entered view range
            if (mo.getType() == MapObjectType.SUMMON || mo.getPosition().distanceSq(chr.getPosition()) <= getRangedDistance())
            {
                chr.addVisibleMapObject(mo);
                mo.sendSpawnData(chr.getClient());
            }
        }
        else if (mo.getType() != MapObjectType.SUMMON && mo.getPosition().distanceSq(chr.getPosition()) > getRangedDistance())
        {
            chr.removeVisibleMapObject(mo);
            mo.sendDestroyData(chr.getClient());
        }
    }

    public void moveMonster(Monster monster, Point reportedPos)
    {
        monster.setPosition(reportedPos);
        foreach (IPlayer chr in getAllPlayers())
        {
            updateMapObjectVisibility(chr, monster);
        }
    }

    public void movePlayer(IPlayer player, Point newPosition)
    {
        player.setPosition(newPosition);

        try
        {
            IMapObject[] visibleObjects = player.getVisibleMapObjects();

            var mapObjects = getCopyMapObjects();
            foreach (IMapObject mo in visibleObjects)
            {
                if (mo != null)
                {
                    if (mapObjects.GetValueOrDefault(mo.getObjectId()) == mo)
                    {
                        updateMapObjectVisibility(player, mo);
                    }
                    else
                    {
                        player.removeVisibleMapObject(mo);
                    }
                }
            }
        }
        catch (Exception e)
        {
            log.Error(e.ToString());
        }

        foreach (var mo in getMapObjectsInRange(player.getPosition(), getRangedDistance(), rangedMapobjectTypes))
        {
            if (!player.isMapObjectVisible(mo))
            {
                mo.sendSpawnData(player.getClient());
                player.addVisibleMapObject(mo);
            }
        }
    }

    public void toggleEnvironment(string ms)
    {
        var env = getEnvironment();

        if (env.ContainsKey(ms))
        {
            moveEnvironment(ms, env[ms] == 1 ? 2 : 1);
        }
        else
        {
            moveEnvironment(ms, 1);
        }
    }

    public void moveEnvironment(string ms, int type)
    {
        broadcastMessage(PacketCreator.environmentMove(ms, type));

        objectLock.EnterWriteLock();
        try
        {
            environment.AddOrUpdate(ms, type);
        }
        finally
        {
            objectLock.ExitWriteLock();
        }
    }

    public IDictionary<string, int> getEnvironment()
    {
        objectLock.EnterReadLock();
        try
        {
            return new Dictionary<string, int>(environment);
        }
        finally
        {
            objectLock.ExitReadLock();
        }
    }

    public string getMapName()
    {
        return mapName;
    }

    public void setMapName(string mapName)
    {
        this.mapName = mapName;
    }

    public string getStreetName()
    {
        return streetName;
    }

    public void setClock(bool hasClock)
    {
        this.clock = hasClock;
    }

    public bool hasClock()
    {
        return clock;
    }

    public void setTown(bool isTown)
    {
        this.town = isTown;
    }

    public bool isTown()
    {
        return town;
    }

    public bool isMuted()
    {
        return _isMuted;
    }

    public void setMuted(bool mute)
    {
        _isMuted = mute;
    }

    public void setStreetName(string streetName)
    {
        this.streetName = streetName;
    }

    public void setEverlast(bool everlast)
    {
        this.everlast = everlast;
    }

    public bool getEverlast()
    {
        return everlast;
    }

    public int getSpawnedMonstersOnMap()
    {
        return spawnedMonstersOnMap.get();
    }

    public void setMobCapacity(int capacity)
    {
        this.mobCapacity = capacity;
    }

    public void setBackgroundTypes(Dictionary<int, int> backTypes)
    {
        backgroundTypes.putAll(backTypes);
    }

    // not really costly to keep generating imo
    public void sendNightEffect(IPlayer chr)
    {
        foreach (var types in backgroundTypes)
        {
            if (types.Value >= 3)
            { // 3 is a special number
                chr.sendPacket(PacketCreator.changeBackgroundEffect(true, types.Key, 0));
            }
        }
    }

    public void broadcastNightEffect()
    {
        chrLock.EnterReadLock();
        try
        {
            foreach (IPlayer chr in this.characters)
            {
                sendNightEffect(chr);
            }
        }
        finally
        {
            chrLock.ExitReadLock();
        }
    }

    public IPlayer? getCharacterByName(string name)
    {
        chrLock.EnterReadLock();
        try
        {
            return characters.FirstOrDefault(x => x.getName().Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            chrLock.ExitReadLock();
        }
    }

    public bool makeDisappearItemFromMap(IMapObject? mapobj)
    {
        if (mapobj is MapItem o)
        {
            return makeDisappearItemFromMap(o);
        }
        else
        {
            return mapobj == null;  // no drop to make disappear...
        }
    }

    public bool makeDisappearItemFromMap(MapItem mapitem)
    {
        if (mapitem != null && mapitem == getMapObject(mapitem.getObjectId()))
        {
            mapitem.lockItem();
            try
            {
                if (mapitem.isPickedUp())
                {
                    return true;
                }

                pickItemDrop(PacketCreator.removeItemFromMap(mapitem.getObjectId(), 0, 0), mapitem);
                return true;
            }
            finally
            {
                mapitem.unlockItem();
            }
        }

        return false;
    }

    public class MobLootEntry : AbstractRunnable
    {

        private byte droptype;
        private int mobpos;
        private int chRate;
        private Point pos;
        private List<MonsterDropEntry> dropEntry;
        private List<MonsterDropEntry> visibleQuestEntry;
        private List<MonsterDropEntry> otherQuestEntry;
        private List<MonsterGlobalDropEntry> globalEntry;
        private IPlayer chr;
        private Monster mob;
        IMap _map;
        public MobLootEntry(IMap map, byte droptype, int mobpos, int chRate, Point pos, List<MonsterDropEntry> dropEntry, List<MonsterDropEntry> visibleQuestEntry, List<MonsterDropEntry> otherQuestEntry, List<MonsterGlobalDropEntry> globalEntry, IPlayer chr, Monster mob)
        {
            _map = map;
            this.droptype = droptype;
            this.mobpos = mobpos;
            this.chRate = chRate;
            this.pos = pos;
            this.dropEntry = dropEntry;
            this.visibleQuestEntry = visibleQuestEntry;
            this.otherQuestEntry = otherQuestEntry;
            this.globalEntry = globalEntry;
            this.chr = chr;
            this.mob = mob;
        }

        public override void HandleRun()
        {
            byte d = 1;

            // Normal Drops
            d = _map.dropItemsFromMonsterOnMap(dropEntry, pos, d, chRate, droptype, mobpos, chr, mob);

            // Global Drops
            d = _map.dropGlobalItemsFromMonsterOnMap(globalEntry, pos, d, droptype, mobpos, chr, mob);

            // Quest Drops
            d = _map.dropItemsFromMonsterOnMap(visibleQuestEntry, pos, d, chRate, droptype, mobpos, chr, mob);
            _map.dropItemsFromMonsterOnMap(otherQuestEntry, pos, d, chRate, droptype, mobpos, chr, mob);
        }
    }

    private class ActivateItemReactor : AbstractRunnable
    {

        private MapItem mapitem;
        private Reactor reactor;
        private IClient c;
        private IMap _map;
        public ActivateItemReactor(IMap map, MapItem mapitem, Reactor reactor, IClient c)
        {
            _map = map;
            this.mapitem = mapitem;
            this.reactor = reactor;
            this.c = c;
        }

        public override void HandleRun()
        {
            reactor.hitLockReactor();
            try
            {
                if (reactor.getReactorType() == 100)
                {
                    if (reactor.getShouldCollect() == true && mapitem != null && mapitem == _map.getMapObject(mapitem.getObjectId()))
                    {
                        mapitem.lockItem();
                        try
                        {
                            if (mapitem.isPickedUp())
                            {
                                return;
                            }
                            mapitem.setPickedUp(true);
                            _map.unregisterItemDrop(mapitem);

                            reactor.setShouldCollect(false);
                            _map.broadcastMessage(PacketCreator.removeItemFromMap(mapitem.getObjectId(), 0, 0), mapitem.getPosition());

                            _map.droppedItemCount.decrementAndGet();
                            _map.removeMapObject(mapitem);

                            reactor.hitReactor(c);

                            if (reactor.getDelay() > 0)
                            {
                                var reactorMap = reactor.getMap();

                                OverallService service = (OverallService)reactorMap.getChannelServer().getServiceAccess(ChannelServices.OVERALL);
                                service.registerOverallAction(reactorMap.getId(), () =>
                                {
                                    reactor.lockReactor();
                                    try
                                    {
                                        reactor.resetReactorActions(0);
                                        reactor.setAlive(true);
                                        _map.broadcastMessage(PacketCreator.triggerReactor(reactor, 0));
                                    }
                                    finally
                                    {
                                        reactor.unlockReactor();
                                    }
                                }, reactor.getDelay());
                            }
                        }
                        finally
                        {
                            mapitem.unlockItem();
                        }
                    }
                }
            }
            finally
            {
                reactor.hitUnlockReactor();
            }
        }
    }

    public void instanceMapFirstSpawn(int difficulty, bool isPq)
    {
        foreach (SpawnPoint spawnPoint in getAllMonsterSpawn())
        {
            if (spawnPoint.getMobTime() == -1)
            {   //just those allowed to be spawned only once
                spawnMonster(spawnPoint.getMonster());
            }
        }
    }

    public void instanceMapRespawn()
    {
        if (!_allowSummons)
        {
            return;
        }

        int numShouldSpawn = (short)((monsterSpawn.Count - spawnedMonstersOnMap.get()));//Fking lol'd
        if (numShouldSpawn > 0)
        {
            var randomSpawn = getMonsterSpawn().ToList();
            Collections.shuffle(randomSpawn);
            int spawned = 0;
            foreach (SpawnPoint spawnPoint in randomSpawn)
            {
                if (spawnPoint.shouldSpawn())
                {
                    spawnMonster(spawnPoint.getMonster());
                    spawned++;
                    if (spawned >= numShouldSpawn)
                    {
                        break;
                    }
                }
            }
        }
    }

    public void instanceMapForceRespawn()
    {
        if (!_allowSummons)
        {
            return;
        }

        int numShouldSpawn = (short)((monsterSpawn.Count - spawnedMonstersOnMap.get()));//Fking lol'd
        if (numShouldSpawn > 0)
        {
            var randomSpawn = getMonsterSpawn().ToList();
            Collections.shuffle(randomSpawn);
            int spawned = 0;
            foreach (SpawnPoint spawnPoint in randomSpawn)
            {
                if (spawnPoint.shouldForceSpawn())
                {
                    spawnMonster(spawnPoint.getMonster());
                    spawned++;
                    if (spawned >= numShouldSpawn)
                    {
                        break;
                    }
                }
            }
        }
    }

    public void closeMapSpawnPoints()
    {
        foreach (SpawnPoint spawnPoint in getMonsterSpawn())
        {
            spawnPoint.setDenySpawn(true);
        }
    }

    public void restoreMapSpawnPoints()
    {
        foreach (SpawnPoint spawnPoint in getMonsterSpawn())
        {
            spawnPoint.setDenySpawn(false);
        }
    }

    public void setAllowSpawnPointInBox(bool allow, Rectangle box)
    {
        foreach (SpawnPoint sp in getMonsterSpawn())
        {
            if (box.Contains(sp.getPosition()))
            {
                sp.setDenySpawn(!allow);
            }
        }
    }

    public void setAllowSpawnPointInRange(bool allow, Point from, double rangeSq)
    {
        foreach (SpawnPoint sp in getMonsterSpawn())
        {
            if (from.distanceSq(sp.getPosition()) <= rangeSq)
            {
                sp.setDenySpawn(!allow);
            }
        }
    }

    public SpawnPoint? findClosestSpawnpoint(Point from)
    {
        SpawnPoint? closest = null;
        double shortestDistance = double.PositiveInfinity;
        foreach (SpawnPoint sp in getMonsterSpawn())
        {
            double distance = sp.getPosition().distanceSq(from);
            if (distance < shortestDistance)
            {
                closest = sp;
                shortestDistance = distance;
            }
        }
        return closest;
    }

    private static double getCurrentSpawnRate(int numPlayers)
    {
        return 0.70 + (0.05 * Math.Min(6, numPlayers));
    }

    private int getNumShouldSpawn(int numPlayers)
    {
        /*
        Console.WriteLine("----------------------------------");
        foreach(SpawnPoint spawnPoint in getMonsterSpawn()) {
            Console.WriteLine("sp " + spawnPoint.getPosition().getX() + ", " + spawnPoint.getPosition().getY() + ": " + spawnPoint.getDenySpawn());
        }
        Console.WriteLine("try " + monsterSpawn.Count + " - " + spawnedMonstersOnMap.get());
        Console.WriteLine("----------------------------------");
        */

        if (YamlConfig.config.server.USE_ENABLE_FULL_RESPAWN)
        {
            return (monsterSpawn.Count - spawnedMonstersOnMap.get());
        }

        int maxNumShouldSpawn = (int)Math.Ceiling(getCurrentSpawnRate(numPlayers) * monsterSpawn.Count);
        return maxNumShouldSpawn - spawnedMonstersOnMap.get();
    }

    public void respawn()
    {
        if (!_allowSummons)
        {
            return;
        }

        int numPlayers;
        chrLock.EnterReadLock();
        try
        {
            numPlayers = characters.Count;

            if (numPlayers == 0)
            {
                return;
            }
        }
        finally
        {
            chrLock.ExitReadLock();
        }

        int numShouldSpawn = getNumShouldSpawn(numPlayers);
        if (numShouldSpawn > 0)
        {
            List<SpawnPoint> randomSpawn = new(getMonsterSpawn());
            Collections.shuffle(randomSpawn);
            short spawned = 0;
            foreach (SpawnPoint spawnPoint in randomSpawn)
            {
                if (spawnPoint.shouldSpawn())
                {
                    spawnMonster(spawnPoint.getMonster());
                    spawned++;

                    if (spawned >= numShouldSpawn)
                    {
                        break;
                    }
                }
            }
        }
    }

    public void mobMpRecovery()
    {
        foreach (Monster mob in this.getAllMonsters())
        {
            if (mob.isAlive())
            {
                mob.heal(0, mob.getLevel());
            }
        }
    }

    public int getNumPlayersInArea(int index)
    {
        return getNumPlayersInRect(getArea(index));
    }

    public int getNumPlayersInRect(Rectangle rect)
    {
        chrLock.EnterReadLock();
        try
        {
            return characters.Count(x => rect.Contains(x.getPosition()));
        }
        finally
        {
            chrLock.ExitReadLock();
        }
    }

    public int getNumPlayersItemsInArea(int index)
    {
        return getNumPlayersItemsInRect(getArea(index));
    }

    public int getNumPlayersItemsInRect(Rectangle rect)
    {
        int retP = getNumPlayersInRect(rect);
        int retI = getMapObjectsInBox(rect, Arrays.asList(MapObjectType.ITEM)).Count;

        return retP + retI;
    }

    //private interface DelayedPacketCreation
    //{
    //    public Action<IClient>? sendPackets { get; set; }
    //}

    //public class ActualDelayedPacketCreation : DelayedPacketCreation
    //{
    //    public Action<IClient>? sendPackets { get; set; }
    //}

    //private class SpawnCondition
    //{

    //    public Func<IPlayer, bool>? canSpawn;
    //}

    public int getHPDec()
    {
        return decHP;
    }

    public void setHPDec(int delta)
    {
        decHP = delta;
    }

    public int getHPDecProtect()
    {
        return protectItem;
    }

    public void setHPDecProtect(int delta)
    {
        this.protectItem = delta;
    }

    public float getRecovery()
    {
        return recovery;
    }

    public void setRecovery(float recRate)
    {
        recovery = recRate;
    }

    private int hasBoat()
    {
        return !boat ? 0 : (docked ? 1 : 2);
    }

    public void setBoat(bool hasBoat)
    {
        this.boat = hasBoat;
    }

    public void setDocked(bool isDocked)
    {
        this.docked = isDocked;
    }

    public bool getDocked()
    {
        return this.docked;
    }

    public void setSeats(int seats)
    {
        this.seats = seats;
    }

    public int getSeats()
    {
        return seats;
    }

    public void broadcastGMMessage(IPlayer source, Packet packet, bool repeatToSource)
    {
        broadcastGMMessage(repeatToSource ? null : source, packet, double.PositiveInfinity, source.getPosition());
    }

    private void broadcastGMMessage(IPlayer? source, Packet packet, double rangeSq, Point? rangedFrom)
    {
        chrLock.EnterReadLock();
        try
        {
            foreach (IPlayer chr in characters)
            {
                if (chr != source && chr.isGM())
                {
                    if (rangeSq < double.PositiveInfinity)
                    {
                        if (rangedFrom != null && rangedFrom.Value.distanceSq(chr.getPosition()) <= rangeSq)
                        {
                            chr.sendPacket(packet);
                        }
                    }
                    else
                    {
                        chr.sendPacket(packet);
                    }
                }
            }
        }
        finally
        {
            chrLock.ExitReadLock();
        }
    }

    public void broadcastNONGMMessage(IPlayer source, Packet packet, bool repeatToSource)
    {
        chrLock.EnterReadLock();
        try
        {
            foreach (IPlayer chr in characters)
            {
                if (chr != source && !chr.isGM())
                {
                    chr.sendPacket(packet);
                }
            }
        }
        finally
        {
            chrLock.ExitReadLock();
        }
    }

    public OxQuiz getOx()
    {
        return ox;
    }

    public void setOx(OxQuiz? set)
    {
        this.ox = set;
    }

    public void setOxQuiz(bool b)
    {
        this._isOxQuiz = b;
    }

    public bool isOxQuiz()
    {
        return _isOxQuiz;
    }

    public void setOnUserEnter(string onUserEnter)
    {
        this.onUserEnter = onUserEnter;
    }

    public string getOnUserEnter()
    {
        return onUserEnter;
    }

    public void setOnFirstUserEnter(string onFirstUserEnter)
    {
        this.onFirstUserEnter = onFirstUserEnter;
    }

    public string getOnFirstUserEnter()
    {
        return onFirstUserEnter;
    }

    private bool hasForcedEquip()
    {
        return fieldType == 81 || fieldType == 82;
    }

    public void setFieldType(int fieldType)
    {
        this.fieldType = fieldType;
    }

    public void clearDrops(IPlayer player)
    {
        foreach (IMapObject i in getMapObjectsInRange(player.getPosition(), double.PositiveInfinity, Arrays.asList(MapObjectType.ITEM)))
        {
            droppedItemCount.decrementAndGet();
            removeMapObject(i);
            this.broadcastMessage(PacketCreator.removeItemFromMap(i.getObjectId(), 0, player.getId()));
        }
    }

    public void clearDrops()
    {
        foreach (IMapObject i in getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.ITEM)))
        {
            droppedItemCount.decrementAndGet();
            removeMapObject(i);
            this.broadcastMessage(PacketCreator.removeItemFromMap(i.getObjectId(), 0, 0));
        }
    }

    public void setFieldLimit(int fieldLimit)
    {
        this.fieldLimit = fieldLimit;
    }

    public int getFieldLimit()
    {
        return fieldLimit;
    }

    public void allowSummonState(bool b)
    {
        _allowSummons = b;
    }

    public bool getSummonState()
    {
        return _allowSummons;
    }

    public void warpEveryone(int to)
    {
        List<IPlayer> players = new(getCharacters());

        foreach (IPlayer chr in players)
        {
            chr.changeMap(to);
        }
    }

    public void warpEveryone(int to, int pto)
    {
        List<IPlayer> players = new(getCharacters());

        foreach (IPlayer chr in players)
        {
            chr.changeMap(to, pto);
        }
    }

    // BEGIN EVENTS
    public void setSnowball(int team, Snowball? ball)
    {
        switch (team)
        {
            case 0:
                this.snowball0 = ball;
                break;
            case 1:
                this.snowball1 = ball;
                break;
            default:
                break;
        }
    }

    public Snowball? getSnowball(int team)
    {
        switch (team)
        {
            case 0:
                return snowball0;
            case 1:
                return snowball1;
            default:
                return null;
        }
    }

    private bool specialEquip()
    {
        //Maybe I shouldn't use fieldType :\
        return fieldType == 4 || fieldType == 19;
    }

    public void setCoconut(Coconut? nut)
    {
        this.coconut = nut;
    }

    public Coconut? getCoconut()
    {
        return coconut;
    }

    public void warpOutByTeam(int team, int mapid)
    {
        List<IPlayer> chars = new(getCharacters());
        foreach (IPlayer chr in chars)
        {
            if (chr != null)
            {
                if (chr.getTeam() == team)
                {
                    chr.changeMap(mapid);
                }
            }
        }
    }

    public void startEvent(IPlayer chr)
    {
        if (this.mapid == MapId.EVENT_COCONUT_HARVEST && getCoconut() == null)
        {
            setCoconut(new Coconut(this));
            coconut.startEvent();
        }
        else if (this.mapid == MapId.EVENT_PHYSICAL_FITNESS)
        {
            chr.setFitness(new Fitness(chr));
            chr.getFitness().startFitness();
        }
        else if (this.mapid == MapId.EVENT_OLA_OLA_1 || this.mapid == MapId.EVENT_OLA_OLA_2 ||
                this.mapid == MapId.EVENT_OLA_OLA_3 || this.mapid == MapId.EVENT_OLA_OLA_4)
        {
            chr.setOla(new Ola(chr));
            chr.getOla()!.startOla();
        }
        else if (this.mapid == MapId.EVENT_OX_QUIZ && getOx() == null)
        {
            setOx(new OxQuiz(this));
            getOx().sendQuestion();
            setOxQuiz(true);
        }
        else if (this.mapid == MapId.EVENT_SNOWBALL && getSnowball(chr.getTeam()) == null)
        {
            setSnowball(0, new Snowball(0, this));
            setSnowball(1, new Snowball(1, this));
            getSnowball(chr.getTeam())?.startEvent();
        }
    }

    public bool eventStarted()
    {
        return eventstarted;
    }

    public void startEvent()
    {
        this.eventstarted = true;
    }

    public void setEventStarted(bool @event)
    {
        this.eventstarted = @event;
    }

    public string? getEventNPC()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("Talk to ");
        if (mapid == MapId.SOUTHPERRY)
        {
            sb.Append("Paul!");
        }
        else if (mapid == MapId.LITH_HARBOUR)
        {
            sb.Append("Jean!");
        }
        else if (mapid == MapId.ORBIS)
        {
            sb.Append("Martin!");
        }
        else if (mapid == MapId.LUDIBRIUM)
        {
            sb.Append("Tony!");
        }
        else
        {
            return null;
        }
        return sb.ToString();
    }

    public bool hasEventNPC()
    {
        return this.mapid == 60000 || this.mapid == MapId.LITH_HARBOUR || this.mapid == MapId.ORBIS || this.mapid == MapId.LUDIBRIUM;
    }

    public bool isStartingEventMap()
    {
        return this.mapid == MapId.EVENT_PHYSICAL_FITNESS || this.mapid == MapId.EVENT_OX_QUIZ ||
                this.mapid == MapId.EVENT_FIND_THE_JEWEL || this.mapid == MapId.EVENT_OLA_OLA_0 || this.mapid == MapId.EVENT_OLA_OLA_1;
    }

    public bool isEventMap()
    {
        return this.mapid >= MapId.EVENT_FIND_THE_JEWEL && this.mapid < MapId.EVENT_WINNER || this.mapid > MapId.EVENT_EXIT && this.mapid <= 109090000;
    }

    public void setTimeMob(int id, string msg)
    {
        timeMob = new(id, msg);
    }

    public KeyValuePair<int, string>? getTimeMob()
    {
        return timeMob;
    }

    public void toggleHiddenNPC(int id)
    {
        chrLock.EnterReadLock();
        objectLock.EnterReadLock();
        try
        {
            foreach (IMapObject obj in getMapObjects())
            {
                if (obj.getType() == MapObjectType.NPC && obj is NPC npc)
                {
                    if (npc.getId() == id)
                    {
                        npc.setHide(!npc.isHidden());
                        if (!npc.isHidden()) //Should only be hidden upon changing maps
                        {
                            broadcastMessage(PacketCreator.spawnNPC(npc));
                        }
                    }
                }
            }
        }
        finally
        {
            objectLock.ExitReadLock();
            chrLock.ExitReadLock();
        }
    }

    public void setMobInterval(short interval)
    {
        this.mobInterval = interval;
    }

    public short getMobInterval()
    {
        return mobInterval;
    }

    public void clearMapObjects()
    {
        clearDrops();
        killAllMonsters();
        resetReactors();
    }

    public void resetFully()
    {
        resetMapObjects();
    }

    public void resetMapObjects()
    {
        resetMapObjects(1, false);
    }

    public void resetPQ()
    {
        resetPQ(1);
    }

    public void resetPQ(int difficulty)
    {
        resetMapObjects(difficulty, true);
    }

    public void resetMapObjects(int difficulty, bool isPq)
    {
        clearMapObjects();

        restoreMapSpawnPoints();
        instanceMapFirstSpawn(difficulty, isPq);
    }

    public void broadcastShip(bool state)
    {
        broadcastMessage(PacketCreator.boatPacket(state));
        this.setDocked(state);
    }

    public void broadcastEnemyShip(bool state)
    {
        broadcastMessage(PacketCreator.crogBoatPacket(state));
        this.setDocked(state);
    }

    public bool isHorntailDefeated()
    {   // all parts of dead horntail can be found here?
        for (int i = MobId.DEAD_HORNTAIL_MIN; i <= MobId.DEAD_HORNTAIL_MAX; i++)
        {
            if (getMonsterById(i) == null)
            {
                return false;
            }
        }

        return true;
    }

    public void spawnHorntailOnGroundBelow(Point targetPoint)
    {   // ayy lmao
        var htIntro = LifeFactory.getMonster(MobId.SUMMON_HORNTAIL)!;
        spawnMonsterOnGroundBelow(htIntro, targetPoint);    // htintro spawn animation converting into horntail detected thanks to Arnah

        var ht = LifeFactory.getMonster(MobId.HORNTAIL);
        ht.setParentMobOid(htIntro.getObjectId());
        ht.addListener(new ActualMonsterListener()
        {
            monsterDamaged = (IPlayer from, int trueDmg) =>
            {
                ht.addHp(trueDmg);
            },

            monsterHealed = (int trueHeal) =>
            {
                ht.addHp(-trueHeal);
            }
        });
        spawnMonsterOnGroundBelow(ht, targetPoint);

        for (int mobId = MobId.HORNTAIL_HEAD_A; mobId <= MobId.HORNTAIL_TAIL; mobId++)
        {
            Monster m = LifeFactory.getMonster(mobId)!;
            m.setParentMobOid(htIntro.getObjectId());

            m.addListener(new ActualMonsterListener()
            {
                monsterDamaged = (IPlayer from, int trueDmg) =>
                {
                    // thanks Halcyon for noticing HT not dropping loots due to propagated damage not registering attacker
                    ht.applyFakeDamage(from, trueDmg, true);
                },

                monsterHealed = (int trueHeal) =>
                {
                    ht.addHp(trueHeal);
                }
            });

            spawnMonsterOnGroundBelow(m, targetPoint);
        }
    }

    public bool claimOwnership(IPlayer chr)
    {
        if (mapOwner == null)
        {
            this.mapOwner = chr;
            chr.setOwnedMap(this);

            mapOwnerLastActivityTime = Server.getInstance().getCurrentTime();

            getChannelServer().registerOwnedMap(this);
            return true;
        }
        else
        {
            return chr == mapOwner;
        }
    }

    public IPlayer? unclaimOwnership()
    {
        var lastOwner = this.mapOwner;
        return unclaimOwnership(lastOwner) ? lastOwner : null;
    }

    public bool unclaimOwnership(IPlayer? chr)
    {
        if (chr != null && mapOwner == chr)
        {
            this.mapOwner = null;
            chr.setOwnedMap(null);

            mapOwnerLastActivityTime = long.MaxValue;

            getChannelServer().unregisterOwnedMap(this);
            return true;
        }
        else
        {
            return false;
        }
    }

    private void refreshOwnership()
    {
        mapOwnerLastActivityTime = Server.getInstance().getCurrentTime();
    }

    public bool isOwnershipRestricted(IPlayer chr)
    {
        IPlayer? owner = mapOwner;

        if (owner != null)
        {
            if (owner != chr && !owner.isPartyMember(chr))
            {    // thanks Vcoc & BHB for suggesting the map ownership feature
                chr.showMapOwnershipInfo(owner);
                return true;
            }
            else
            {
                this.refreshOwnership();
            }
        }

        return false;
    }

    public void checkMapOwnerActivity()
    {
        long timeNow = Server.getInstance().getCurrentTime();
        if (timeNow - mapOwnerLastActivityTime > 60000)
        {
            if (unclaimOwnership() != null)
            {
                this.dropMessage(5, "This lawn is now free real estate.");
            }
        }
    }

    private List<Point> takenSpawns = new();
    private List<GuardianSpawnPoint> guardianSpawns = new();
    private List<MCSkill> blueTeamBuffs = new();
    private List<MCSkill> redTeamBuffs = new();
    private List<int> skillIds = new();
    private List<KeyValuePair<int, int>> mobsToSpawn = new();

    public List<MCSkill> getBlueTeamBuffs()
    {
        return blueTeamBuffs;
    }

    public List<MCSkill> getRedTeamBuffs()
    {
        return redTeamBuffs;
    }

    public void clearBuffList()
    {
        redTeamBuffs.Clear();
        blueTeamBuffs.Clear();
    }

    public List<IMapObject> getAllPlayer()
    {
        return getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.PLAYER));
    }

    public bool isCPQMap()
    {
        switch (this.getId())
        {
            case 980000101:
            case 980000201:
            case 980000301:
            case 980000401:
            case 980000501:
            case 980000601:
            case 980031100:
            case 980032100:
            case 980033100:
                return true;
        }
        return false;
    }

    public bool isCPQMap2()
    {
        switch (this.getId())
        {
            case 980031100:
            case 980032100:
            case 980033100:
                return true;
        }
        return false;
    }

    public bool isCPQLobby()
    {
        switch (this.getId())
        {
            case 980000100:
            case 980000200:
            case 980000300:
            case 980000400:
            case 980000500:
            case 980000600:
                return true;
        }
        return false;
    }

    public bool isBlueCPQMap()
    {
        switch (this.getId())
        {
            case 980000501:
            case 980000601:
            case 980031200:
            case 980032200:
            case 980033200:
                return true;
        }
        return false;
    }

    public bool isPurpleCPQMap()
    {
        switch (this.getId())
        {
            case 980000301:
            case 980000401:
            case 980031200:
            case 980032200:
            case 980033200:
                return true;
        }
        return false;
    }

    public Point? getRandomSP(int team)
    {
        if (takenSpawns.Count > 0)
        {
            foreach (SpawnPoint sp in monsterSpawn)
            {
                foreach (Point pt in takenSpawns)
                {
                    if ((sp.getPosition().X == pt.X && sp.getPosition().Y == pt.Y) || (sp.getTeam() != team && !this.isBlueCPQMap()))
                    {
                        continue;
                    }
                    else
                    {
                        takenSpawns.Add(pt);
                        return sp.getPosition();
                    }
                }
            }
        }
        else
        {
            foreach (SpawnPoint sp in monsterSpawn)
            {
                if (sp.getTeam() == team || this.isBlueCPQMap())
                {
                    takenSpawns.Add(sp.getPosition());
                    return sp.getPosition();
                }
            }
        }
        return null;
    }

    public GuardianSpawnPoint? getRandomGuardianSpawn(int team)
    {
        bool alltaken = false;
        foreach (GuardianSpawnPoint a in this.guardianSpawns)
        {
            if (!a.isTaken())
            {
                alltaken = false;
                break;
            }
        }
        if (alltaken)
        {
            return null;
        }
        if (this.guardianSpawns.Count > 0)
        {
            while (true)
            {
                foreach (GuardianSpawnPoint gsp in this.guardianSpawns)
                {
                    if (!gsp.isTaken() && Randomizer.nextDouble() < 0.3 && (gsp.getTeam() == -1 || gsp.getTeam() == team))
                    {
                        return gsp;
                    }
                }
            }
        }
        return null;
    }

    public void addGuardianSpawnPoint(GuardianSpawnPoint a)
    {
        this.guardianSpawns.Add(a);
    }

    public int spawnGuardian(int team, int num)
    {
        try
        {
            if (team == 0 && redTeamBuffs.Count >= 4 || team == 1 && blueTeamBuffs.Count >= 4)
            {
                return 2;
            }
            MCSkill? skill = CarnivalFactory.getInstance().getGuardian(num);
            if (skill == null)
                return 0;

            if (team == 0 && redTeamBuffs.Contains(skill))
            {
                return 0;
            }
            else if (team == 1 && blueTeamBuffs.Contains(skill))
            {
                return 0;
            }
            var pt = this.getRandomGuardianSpawn(team);
            if (pt == null)
            {
                return -1;
            }
            int reactorID = 9980000 + team;
            Reactor reactor = new Reactor(ReactorFactory.getReactorS(reactorID), reactorID);
            pt.setTaken(true);
            reactor.setPosition(pt.getPosition());
            reactor.setName(team + "" + num); //lol
            reactor.resetReactorActions(0);
            this.spawnReactor(reactor);
            reactor.setGuardian(pt);
            this.buffMonsters(team, skill);
            getReactorByOid(reactor.getObjectId()).hitReactor(((IPlayer)this.getAllPlayer().get(0)).getClient());
        }
        catch (Exception e)
        {
            log.Error(e.ToString());
        }
        return 1;
    }

    public void buffMonsters(int team, MCSkill skill)
    {
        if (skill == null)
        {
            return;
        }

        if (team == 0)
        {
            redTeamBuffs.Add(skill);
        }
        else if (team == 1)
        {
            blueTeamBuffs.Add(skill);
        }
        foreach (IMapObject mmo in getMapObjects())
        {
            if (mmo.getType() == MapObjectType.MONSTER)
            {
                Monster mob = (Monster)mmo;
                if (mob.getTeam() == team)
                {
                    skill.getSkill().applyEffect(null, mob, false, null);
                }
            }
        }
    }

    public List<int> getSkillIds()
    {
        return skillIds;
    }

    public void addSkillId(int z)
    {
        this.skillIds.Add(z);
    }

    public void addMobSpawn(int mobId, int spendCP)
    {
        this.mobsToSpawn.Add(new KeyValuePair<int, int>(mobId, spendCP));
    }

    public List<KeyValuePair<int, int>> getMobsToSpawn()
    {
        return mobsToSpawn;
    }

    public bool isCPQWinnerMap()
    {
        switch (this.getId())
        {
            case 980000103:
            case 980000203:
            case 980000303:
            case 980000403:
            case 980000503:
            case 980000603:
            case 980031300:
            case 980032300:
            case 980033300:
                return true;
        }
        return false;
    }

    public bool isCPQLoserMap()
    {
        switch (this.getId())
        {
            case 980000104:
            case 980000204:
            case 980000304:
            case 980000404:
            case 980000504:
            case 980000604:
            case 980031400:
            case 980032400:
            case 980033400:
                return true;
        }
        return false;
    }

    public void runCharacterStatUpdate()
    {
        if (statUpdateRunnables.Count > 0)
        {
            List<Action> toRun = new(statUpdateRunnables);
            statUpdateRunnables.Clear();

            foreach (Action r in toRun)
            {
                r.Invoke();
            }
        }
    }

    public void registerCharacterStatUpdate(Action r)
    {
        statUpdateRunnables.Add(r);
    }

    public void dispose()
    {
        foreach (Monster mm in this.getAllMonsters())
        {
            mm.dispose();
        }

        clearMapObjects();


        @event = null;
        footholds = null;
        portals.Clear();
        mapEffect = null;

        chrLock.EnterWriteLock();
        try
        {
            if (aggroMonitor != null)
            {
                aggroMonitor.dispose();
                aggroMonitor = null;
            }

            if (itemMonitor != null)
            {
                itemMonitor.cancel(false);
                itemMonitor = null;
            }

            if (expireItemsTask != null)
            {
                expireItemsTask.cancel(false);
                expireItemsTask = null;
            }

            if (mobSpawnLootTask != null)
            {
                mobSpawnLootTask.cancel(false);
                mobSpawnLootTask = null;
            }

            if (characterStatUpdateTask != null)
            {
                characterStatUpdateTask.cancel(false);
                characterStatUpdateTask = null;
            }
        }
        finally
        {
            chrLock.ExitWriteLock();
        }
    }

    public int getMaxMobs()
    {
        return maxMobs;
    }

    public void setMaxMobs(int maxMobs)
    {
        this.maxMobs = maxMobs;
    }

    public int getMaxReactors()
    {
        return maxReactors;
    }

    public void setMaxReactors(int maxReactors)
    {
        this.maxReactors = maxReactors;
    }

    public int getDeathCP()
    {
        return deathCP;
    }

    public void setDeathCP(int deathCP)
    {
        this.deathCP = deathCP;
    }

    public int getTimeDefault()
    {
        return timeDefault;
    }

    public void setTimeDefault(int timeDefault)
    {
        this.timeDefault = timeDefault;
    }

    public int getTimeExpand()
    {
        return timeExpand;
    }

    public void setTimeExpand(int timeExpand)
    {
        this.timeExpand = timeExpand;
    }

}