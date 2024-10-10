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


using Application.Core.Game.Maps;
using client;
using client.inventory;
using constants.game;
using constants.id;
using Microsoft.EntityFrameworkCore;
using net.server;
using server.life;
using server.life.positioner;
using server.maps;
using tools;

namespace Application.Core.Game.Life;



/**
 * @author XoticStory
 * @author Ronan
 */
// TODO: remove dependency on custom Npc.wz. All NPCs with id 9901910 and above are custom additions for player npcs.
// In summary: NPCs 9901910-9906599 and 9977777 are custom additions to HeavenMS that should be removed.
public class PlayerNPC : AbstractMapObject
{
    static ILogger log = LogFactory.GetLogger("PlayerNPC");
    private static Dictionary<Byte, List<int>> availablePlayerNpcScriptIds = new();
    private static AtomicInteger runningOverallRank = new AtomicInteger();
    private static List<AtomicInteger> runningWorldRank = new();
    private static Dictionary<KeyValuePair<int, int>, AtomicInteger> runningWorldJobRank = new();

    private Dictionary<short, int> equips = new();
    private int scriptId, face, hair, gender, job;
    private byte skin;
    private string name = "";
    private int dir, FH, RX0, RX1, CY;
    private int worldRank, overallRank, worldJobRank, overallJobRank;

    public PlayerNPC(string name, int scriptId, int face, int hair, int gender, byte skin, Dictionary<short, int> equips, int dir, int FH, int RX0, int RX1, int CX, int CY, int oid)
    {
        this.equips = equips;
        this.scriptId = scriptId;
        this.face = face;
        this.hair = hair;
        this.gender = gender;
        this.skin = skin;
        this.name = name;
        this.dir = dir;
        this.FH = FH;
        this.RX0 = RX0;
        this.RX1 = RX1;
        this.CY = CY;
        this.job = 7777;    // supposed to be developer

        setPosition(new Point(CX, CY));
        setObjectId(oid);
    }

    public PlayerNPC(Playernpc rs, List<PlayernpcsEquip>? equipsFromDB = null)
    {
        try
        {
            CY = rs.Cy;
            name = rs.Name;
            hair = rs.Hair;
            face = rs.Face;
            skin = (byte)rs.Skin;
            gender = rs.Gender;
            dir = rs.Dir;
            FH = rs.Fh;
            RX0 = rs.Rx0;
            RX1 = rs.Rx1;
            scriptId = rs.Scriptid;

            worldRank = rs.Worldrank;
            overallRank = rs.Overallrank;
            worldJobRank = rs.Worldjobrank;
            overallJobRank = GameConstants.getOverallJobRankByScriptId(scriptId);
            job = rs.Job;

            setPosition(new Point(rs.X, CY));
            setObjectId(rs.Id);

            if (equipsFromDB == null)
            {
                using var dbContext = new DBContext();
                equipsFromDB = dbContext.PlayernpcsEquips.Where(x => x.Npcid == rs.Id).ToList();
            }
            equips = equipsFromDB.ToDictionary(x => (short)x.Equippos, x => x.Equipid);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e.ToString());
        }
    }

    public static void loadRunningRankData(DBContext dbContext, int worlds)
    {
        getRunningOverallRanks(dbContext);
        getRunningWorldRanks(dbContext, worlds);
        getRunningWorldJobRanks(dbContext);
    }

    public Dictionary<short, int> getEquips()
    {
        return equips;
    }

    public int getScriptId()
    {
        return scriptId;
    }

    public int getJob()
    {
        return job;
    }

    public int getDirection()
    {
        return dir;
    }

    public int getFH()
    {
        return FH;
    }

    public int getRX0()
    {
        return RX0;
    }

    public int getRX1()
    {
        return RX1;
    }

    public int getCY()
    {
        return CY;
    }

    public byte getSkin()
    {
        return skin;
    }

    public string getName()
    {
        return name;
    }

    public int getFace()
    {
        return face;
    }

    public int getHair()
    {
        return hair;
    }

    public int getGender()
    {
        return gender;
    }

    public int getWorldRank()
    {
        return worldRank;
    }

    public int getOverallRank()
    {
        return overallRank;
    }

    public int getWorldJobRank()
    {
        return worldJobRank;
    }

    public int getOverallJobRank()
    {
        return overallJobRank;
    }

    public override MapObjectType getType()
    {
        return MapObjectType.PLAYER_NPC;
    }

    public override void sendSpawnData(IClient client)
    {
        client.sendPacket(PacketCreator.spawnPlayerNPC(this));
        client.sendPacket(PacketCreator.getPlayerNPC(this));
    }

    public override void sendDestroyData(IClient client)
    {
        client.sendPacket(PacketCreator.removeNPCController(this.getObjectId()));
        client.sendPacket(PacketCreator.removePlayerNPC(this.getObjectId()));
    }

    private static void getRunningOverallRanks(DBContext dbContext)
    {
        var value = dbContext.Playernpcs.Count() == 0 ? 0 : dbContext.Playernpcs.Max(x => x.Overallrank);
        runningOverallRank.set(value + 1);
    }

    private static void getRunningWorldRanks(DBContext dbContext, int worlds)
    {
        int numWorlds = Server.getInstance().getWorldsSize();
        for (int i = 0; i < numWorlds; i++)
        {
            runningWorldRank.Add(new AtomicInteger(1));
        }
        var dataList = dbContext.Playernpcs.GroupBy(x => x.World).Select(x => new { World = x.Key, WorldRank = x.Max(y => y.Worldrank) }).OrderBy(x => x.World);
        foreach (var item in dataList)
        {
            int wid = item.World;
            if (wid < numWorlds)
            {
                runningWorldRank[wid].set(item.WorldRank + 1);
            }
        }
    }

    private static void getRunningWorldJobRanks(DBContext dbContext)
    {
        var dataList = dbContext.Playernpcs
           .GroupBy(x => new { x.World, x.Job })
           .Select(x => new { x.Key.World, x.Key.Job, Worldjobrank = x.Max(y => y.Worldjobrank) })
           .OrderBy(x => x.World)
           .ThenBy(x => x.Job)
           .ToList()
           .Select(x => new KeyValuePair<KeyValuePair<int, int>, AtomicInteger>(new KeyValuePair<int, int>(x.World, x.Job), new AtomicInteger(x.Worldjobrank + 1)))
           .ToList();
        foreach (var item in dataList)
        {
            runningWorldJobRank.AddOrUpdate(item.Key, item.Value);
        }
    }

    private static int getAndIncrementRunningWorldJobRanks(int world, int job)
    {
        var wjr = runningWorldJobRank.GetValueOrDefault(new(world, job));
        if (wjr == null)
        {
            wjr = new AtomicInteger(1);
            runningWorldJobRank.AddOrUpdate(new(world, job), wjr);
        }

        return wjr.getAndIncrement();
    }

    public static bool canSpawnPlayerNpc(string name, int mapid)
    {
        try
        {
            using var dbContext = new DBContext();
            return !dbContext.Playernpcs.Where(x => Microsoft.EntityFrameworkCore.EF.Functions.Like(x.Name, name) && x.Map == mapid).Any();
        }
        catch (Exception e)
        {
            Log.Logger.Error(e.ToString());
        }

        return true;
    }

    public void updatePlayerNPCPosition(IMap map, Point newPos)
    {
        setPosition(newPos);
        RX0 = newPos.X + 50;
        RX1 = newPos.X - 50;
        CY = newPos.Y;
        FH = map.getFootholds().findBelow(newPos)!.getId();

        try
        {
            using var dbContext = new DBContext();
            dbContext.Playernpcs.Where(x => x.Id == getObjectId())
                .ExecuteUpdate(x => x.SetProperty(y => y.X, newPos.X)
                    .SetProperty(y => y.Cy, CY)
                    .SetProperty(y => y.Fh, FH)
                    .SetProperty(y => y.Rx0, RX0)
                    .SetProperty(y => y.Rx1, RX1));
        }
        catch (Exception e)
        {
            Log.Logger.Error(e.ToString());
        }
    }

    private static void fetchAvailableScriptIdsFromDb(byte branch, List<int> list)
    {
        try
        {
            int branchLen = (branch < 26) ? 100 : 400;
            int branchSid = NpcId.PLAYER_NPC_BASE + (branch * 100);
            int nextBranchSid = branchSid + branchLen;

            List<int> availables = new(20);

            using var dbContext = new DBContext();
            HashSet<int> usedScriptIds = dbContext.Playernpcs.Where(x => x.Scriptid >= branchSid && x.Scriptid < nextBranchSid).OrderBy(x => x.Scriptid).Select(x => x.Id).ToHashSet();

            int j = 0;
            for (int i = branchSid; i < nextBranchSid; i++)
            {
                if (!usedScriptIds.Contains(i))
                {
                    if (PlayerNPCFactory.isExistentScriptid(i))
                    {  // thanks Ark, Zein, geno, Ariel, JrCl0wn for noticing client crashes due to use of missing scriptids
                        availables.Add(i);
                        j++;

                        if (j == 20)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;  // after this point no more scriptids expected...
                    }
                }
            }


            for (int i = availables.Count - 1; i >= 0; i--)
            {
                list.Add(availables[i]);
            }
        }
        catch (Exception sqle)
        {
            Log.Logger.Error(sqle.ToString());
        }
    }

    private static int getNextScriptId(byte branch)
    {
        var availablesBranch = availablePlayerNpcScriptIds.GetValueOrDefault(branch);

        if (availablesBranch == null)
        {
            availablesBranch = new(20);
            availablePlayerNpcScriptIds.AddOrUpdate(branch, availablesBranch);
        }

        if (availablesBranch.Count == 0)
        {
            fetchAvailableScriptIdsFromDb(branch, availablesBranch);

            if (availablesBranch.Count == 0)
            {
                return -1;
            }
        }

        return availablesBranch.remove(availablesBranch.Count - 1);
    }

    private static PlayerNPC? createPlayerNPCInternal(IMap map, Point? pos, IPlayer chr)
    {
        lock (proLock)
        {


            int mapId = map.getId();

            if (!canSpawnPlayerNpc(chr.getName(), mapId))
            {
                return null;
            }

            byte branch = GameConstants.getHallOfFameBranch(chr.getJob(), mapId);

            int scriptId = getNextScriptId(branch);
            if (scriptId == -1)
            {
                return null;
            }

            if (pos == null)
            {
                if (GameConstants.isPodiumHallOfFameMap(map.getId()))
                {
                    pos = PlayerNPCPodium.getNextPlayerNpcPosition(map);
                }
                else
                {
                    pos = PlayerNPCPositioner.getNextPlayerNpcPosition(map);
                }

                if (pos == null)
                {
                    return null;
                }
            }

            if (YamlConfig.config.server.USE_DEBUG)
            {
                log.Debug("GOT SID {ScriptId}, POS {Position}", scriptId, pos);
            }

            int worldId = chr.getWorld();
            int jobId = (chr.getJob().getId() / 100) * 100;

            PlayerNPC? ret;

            try
            {
                using var dbContext = new DBContext();
                using var dbTrans = dbContext.Database.BeginTransaction();
                var dbModel = dbContext.Playernpcs.Where(x => x.Scriptid == scriptId).First();

                if (dbModel == null)
                {
                    int npcId;
                    // creates new playernpc if scriptid doesn't exist
                    dbModel = new Playernpc()
                    {
                        Name = chr.getName(),
                        Hair = chr.getHair(),
                        Face = chr.getFace(),
                        Skin = (int)chr.getSkinColor(),
                        Gender = chr.getGender(),
                        X = pos.Value.X,
                        Cy = pos.Value.Y,
                        World = worldId,
                        Map = mapId,
                        Scriptid = scriptId,
                        Dir = 1,
                        Fh = map.getFootholds().findBelow(pos.Value).getId(),
                        Rx0 = pos.Value.X + 50,
                        Rx1 = pos.Value.X - 50,
                        Worldrank = runningWorldRank[worldId].getAndIncrement(),
                        Overallrank = runningOverallRank.getAndIncrement(),
                        Worldjobrank = getAndIncrementRunningWorldJobRanks(worldId, jobId),
                        Job = jobId
                    };
                    dbContext.Playernpcs.Add(dbModel);
                    dbContext.SaveChanges();

                    npcId = dbModel.Id;


                    List<PlayernpcsEquip> equips = new List<PlayernpcsEquip>();
                    foreach (Item equip in chr.getInventory(InventoryType.EQUIPPED))
                    {
                        int position = Math.Abs(equip.getPosition());
                        if ((position < 12 && position > 0) || (position > 100 && position < 112))
                        {
                            equips.Add(new PlayernpcsEquip()
                            {
                                Npcid = npcId,
                                Equipid = equip.getItemId(),
                                Equippos = equip.getPosition()
                            });

                        }
                    }
                    dbContext.PlayernpcsEquips.AddRange(equips);
                    dbContext.SaveChanges();
                    dbTrans.Commit();
                    ret = new PlayerNPC(dbModel, equips);
                }
                else
                {
                    ret = null;
                }

                return ret;
            }
            catch (Exception e)
            {
                Log.Logger.Error(e.ToString());
                return null;
            }
        }
    }

    private static List<int> removePlayerNPCInternal(IMap? map, IPlayer chr)
    {
        lock (proLock)
        {
            HashSet<int> mapids = new() { chr.getWorld() };

            try
            {
                using var dbContext = new DBContext();
                var queryExpression = dbContext.Playernpcs.Where(x => Microsoft.EntityFrameworkCore.EF.Functions.Like(x.Name, chr.getName()));
                if (map != null)
                    queryExpression = queryExpression.Where(x => x.Map == map.getId());

                var dataList = queryExpression.Select(x => new { x.Id, x.Map }).ToList();

                var npcIdList = dataList.Select(x => x.Id).ToList();
                dbContext.Playernpcs.Where(x => npcIdList.Contains(x.Id)).ExecuteDelete();
                dbContext.PlayernpcsEquips.Where(x => npcIdList.Contains(x.Npcid)).ExecuteDelete();
                mapids.addAll<int>(dataList.Select(x => x.Map).ToArray());

            }
            catch (Exception e)
            {
                Log.Logger.Error(e.ToString());
            }
            return mapids.ToList();
        }
    }


    static object proLock = new object();
    public static bool spawnPlayerNPC(int mapid, IPlayer chr)
    {
        return spawnPlayerNPC(mapid, null, chr);
    }

    public static bool spawnPlayerNPC(int mapid, Point? pos, IPlayer? chr)
    {
        if (chr == null)
        {
            return false;
        }

        var pn = createPlayerNPCInternal(chr.getClient().getChannelServer().getMapFactory().getMap(mapid), pos, chr);
        if (pn != null)
        {
            foreach (var channel in Server.getInstance().getChannelsFromWorld(chr.getWorld()))
            {
                var m = channel.getMapFactory().getMap(mapid);

                m.addPlayerNPCMapObject(pn);
                m.broadcastMessage(PacketCreator.spawnPlayerNPC(pn));
                m.broadcastMessage(PacketCreator.getPlayerNPC(pn));
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    private static PlayerNPC? getPlayerNPCFromWorldMap(string name, int world, int map)
    {
        var wserv = Server.getInstance().getWorld(world);
        foreach (var pnpcObj in wserv.getChannel(1).getMapFactory().getMap(map).getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.PLAYER_NPC)))
        {
            PlayerNPC pn = (PlayerNPC)pnpcObj;

            if (name == (pn.getName()) && pn.getScriptId() < NpcId.CUSTOM_DEV)
            {
                return pn;
            }
        }

        return null;
    }

    public static void removePlayerNPC(IPlayer? chr)
    {
        if (chr == null)
        {
            return;
        }

        List<int> updateMapids = removePlayerNPCInternal(null, chr);
        int worldid = updateMapids.remove(0);

        foreach (int mapid in updateMapids)
        {
            var pn = getPlayerNPCFromWorldMap(chr.getName(), worldid, mapid);

            if (pn != null)
            {
                foreach (var channel in Server.getInstance().getChannelsFromWorld(worldid))
                {
                    var m = channel.getMapFactory().getMap(mapid);
                    m.removeMapObject(pn);

                    m.broadcastMessage(PacketCreator.removeNPCController(pn.getObjectId()));
                    m.broadcastMessage(PacketCreator.removePlayerNPC(pn.getObjectId()));
                }
            }
        }
    }

    public static void multicastSpawnPlayerNPC(int mapid, int world)
    {
        var wserv = Server.getInstance().getWorld(world);
        if (wserv == null)
        {
            return;
        }

        var c = Client.createMock();
        c.setWorld(world);
        c.setChannel(1);

        foreach (IPlayer mc in wserv.loadAndGetAllCharactersView())
        {
            mc.setClient(c);
            spawnPlayerNPC(mapid, mc);
        }
    }

    public static void removeAllPlayerNPC()
    {
        try
        {
            using var dbContext = new DBContext();
            var dataList = dbContext.Playernpcs.GroupBy(x => new { x.World, x.Map }).ToList().Select(x => x.Key).ToList();
            int wsize = Server.getInstance().getWorldsSize();
            foreach (var item in dataList)
            {
                int world = item.World, map = item.Map;
                if (world >= wsize)
                {
                    continue;
                }

                foreach (var channel in Server.getInstance().getChannelsFromWorld(world))
                {
                    var m = channel.getMapFactory().getMap(map);

                    foreach (var pnpcObj in m.getMapObjectsInRange(new Point(0, 0), double.PositiveInfinity, Arrays.asList(MapObjectType.PLAYER_NPC)))
                    {
                        PlayerNPC pn = (PlayerNPC)pnpcObj;
                        m.removeMapObject(pnpcObj);
                        m.broadcastMessage(PacketCreator.removeNPCController(pn.getObjectId()));
                        m.broadcastMessage(PacketCreator.removePlayerNPC(pn.getObjectId()));
                    }
                }
            }

            dbContext.Playernpcs.ExecuteDelete();
            dbContext.PlayernpcsEquips.ExecuteDelete();
            dbContext.PlayernpcsFields.ExecuteDelete();
            foreach (var w in Server.getInstance().getWorlds())
            {
                w.resetPlayerNpcMapData();
            }
        }
        catch (Exception e)
        {
            Log.Logger.Error(e.ToString());
        }
    }
}
