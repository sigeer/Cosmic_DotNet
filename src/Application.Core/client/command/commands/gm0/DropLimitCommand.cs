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
   @Author: Arthur L - Refactored command content into modules
*/

namespace client.command.commands.gm0;

public class DropLimitCommand : Command
{
    public override void execute(IClient c, string[] paramValues)
    {
        int dropCount = c.OnlinedCharacter.getMap().getDroppedItemCount();
        if (((float)dropCount) / YamlConfig.config.server.ITEM_LIMIT_ON_MAP < 0.75f)
        {
            c.OnlinedCharacter.showHint("Current drop count: #b" + dropCount + "#k / #e" + YamlConfig.config.server.ITEM_LIMIT_ON_MAP + "#n", 300);
        }
        else
        {
            c.OnlinedCharacter.showHint("Current drop count: #r" + dropCount + "#k / #e" + YamlConfig.config.server.ITEM_LIMIT_ON_MAP + "#n", 300);
        }

    }
}
