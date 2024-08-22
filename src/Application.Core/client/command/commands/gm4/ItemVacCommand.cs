/*
    This file is part of the HeavenMS MapleStory Server, commands OdinMS-based
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
   @Author: Arthur L - Refactored command content into modules
*/


using server.maps;

namespace client.command.commands.gm4;




public class ItemVacCommand : Command
{
    public ItemVacCommand()
    {
        setDescription("Loot all drops on the map.");
    }

    public override void execute(Client c, string[] paramsValue)
    {
        Character player = c.getPlayer();
        List<MapObject> list = player.getMap().getMapObjectsInRange(player.getPosition(), double.PositiveInfinity, Arrays.asList(MapObjectType.ITEM));
        foreach (MapObject item in list)
        {
            player.pickupItem(item);
        }
    }
}
