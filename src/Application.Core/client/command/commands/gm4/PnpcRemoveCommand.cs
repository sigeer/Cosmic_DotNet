/*
    This file is part of the HeavenMS MapleStory NewServer, commands OdinMS-based
    Copyleft (L) 2016 - 2019 RonanLana

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

/*
   @Author: Ronan
*/

using Application.Core.constants.game;

namespace client.command.commands.gm4;


public class PnpcRemoveCommand : Command
{
    public PnpcRemoveCommand()
    {
        setDescription("Remove a permanent NPC on the map.");
    }

    public override void execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        int mapId = player.getMapId();
        int npcId = paramsValue.Length > 0 ? int.Parse(paramsValue[0]) : -1;

        Point pos = player.getPosition();
        int xpos = pos.X;
        int ypos = pos.Y;

        try
        {
            using var dbContext = new DBContext();
            var preSearch = dbContext.Plives.Where(x => x.World == player.getWorld() && x.Map == mapId && x.Type == LifeType.NPC);

            if (npcId > -1)
            {
                preSearch = preSearch.Where(x => x.Life == npcId);
            }
            else
            {
                preSearch = preSearch.Where(x => x.X >= xpos - 50 && x.X <= xpos + 50 && x.Y >= ypos - 50 && x.Y <= ypos + 50);
            }

            var dataList = preSearch.ToList();
            dbContext.Plives.RemoveRange(dataList);
            var toRemove = dataList.Select(x => new { x.Life, x.X, x.Y }).ToList();


            if (toRemove.Count > 0)
            {
                foreach (var ch in player.getWorldServer().getChannels())
                {
                    var map = ch.getMapFactory().getMap(mapId);

                    foreach (var r in toRemove)
                    {
                        map.destroyNPC(r.Life);
                    }
                }
            }

            player.yellowMessage("Cleared " + toRemove.Count + " pNPC placements.");
        }
        catch (Exception e)
        {
            log.Error(e.ToString());
            player.dropMessage(5, "Failed to remove pNPC from the database.");
        }
    }
}