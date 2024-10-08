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

namespace client.command.commands.gm6;

public class SetGmLevelCommand : Command
{
    public SetGmLevelCommand()
    {
        setDescription("Set GM level of a player.");
    }

    public override void execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 2)
        {
            player.yellowMessage("Syntax: !setgmlevel <playername> <newlevel>");
            return;
        }

        int newLevel = int.Parse(paramsValue[1]);
        var target = c.getChannelServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (target != null)
        {
            target.setGMLevel(newLevel);
            target.getClient().setGMLevel(newLevel);

            target.dropMessage("You are now a level " + newLevel + " GM. See @commands for a list of available commands.");
            player.dropMessage(target + " is now a level " + newLevel + " GM.");
        }
        else
        {
            player.dropMessage("Player '" + paramsValue[0] + "' was not found on this channel.");
        }
    }
}
