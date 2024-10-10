using Application.Core.constants.game;
using Application.Core.Game.Life;
using Application.Core.Game.Players;
using constants.id;
using Google.Protobuf.WellKnownTypes;
using server.life;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using tools;

namespace Application.Core.Managers
{
    public class PLifeManager
    {
        public static bool CreatePnpc(int npcId, IPlayer player)
        {
            var npc = LifeFactory.getNPC(npcId);
            if (npc == null || npc.getName() == "MISSINGNO")
                return false;

            int mapId = player.getMapId();

            var checkpos = player.getMap().getGroundBelow(player.getPosition());
            int xpos = checkpos.X;
            int ypos = checkpos.Y;
            int fh = player.getMap().getFootholds()!.findBelow(checkpos)!.getId();

            using var dbContext = new DBContext();
            using var dbTrans = dbContext.Database.BeginTransaction();
            var model = new Plife(player.getWorld(), mapId, npcId, -1, xpos, ypos, fh, LifeType.NPC);
            dbContext.Plives.Add(model);
            dbContext.SaveChanges();

            foreach (var ch in player.getWorldServer().getChannels())
            {
                npc = LifeFactory.getNPC(npcId);
                npc.setPosition(checkpos);
                npc.setCy(ypos);
                npc.setRx0(xpos + 50);
                npc.setRx1(xpos - 50);
                npc.setFh(fh);

                var map = ch.getMapFactory().getMap(mapId);
                map.addMapObject(npc);
                map.broadcastMessage(PacketCreator.spawnNPC(npc));
            }
            dbTrans.Commit();
            return true;
        }

        public static bool RemovePMonster(int mobId, IPlayer player)
        {
            int mapId = player.getMapId();

            Point pos = player.getPosition();
            int xpos = pos.X;
            int ypos = pos.Y;

            using var dbContext = new DBContext();
            var preSearch = dbContext.Plives.Where(x => x.World == player.getWorld() && x.Map == mapId && x.Type == LifeType.Monster);
            if (mobId > -1)
            {
                preSearch = preSearch.Where(x => x.Life == mobId);
            }
            else
            {
                preSearch = preSearch.Where(x => x.X >= xpos - 50 && x.X <= xpos + 50 && x.Y >= ypos - 50 && x.Y <= ypos + 50);
            }

            var dataList = preSearch.ToList();
            dbContext.Plives.RemoveRange(dataList);
            dbContext.SaveChanges();

            var toRemove = dataList.Select(x => new { x.Life, x.X, x.Y }).ToList();


            if (toRemove.Count > 0)
            {
                foreach (var ch in player.getWorldServer().getChannels())
                {
                    var map = ch.getMapFactory().getMap(mapId);

                    foreach (var r in toRemove)
                    {
                        map.removeMonsterSpawn(r.Life, r.X, r.Y);
                        map.removeAllMonsterSpawn(r.Life, r.X, r.Y);
                    }
                }
            }
            return true;
        }
    }
}
