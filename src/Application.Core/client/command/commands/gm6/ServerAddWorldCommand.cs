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


using net.server;
using server;

namespace client.command.commands.gm6;

public class ServerAddWorldCommand : Command
{
    public ServerAddWorldCommand()
    {
        setDescription("Add a new world.");
    }

    public override void execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        ThreadManager.getInstance().newTask(() =>
        {
            int wid = Server.getInstance().addWorld();

            if (player.isLoggedinWorld())
            {
                if (wid >= 0)
                {
                    player.dropMessage(5, "NEW World " + wid + " successfully deployed.");
                }
                else
                {
                    if (wid == -2)
                    {
                        player.dropMessage(5, "Error detected when loading the 'world.ini' file. World creation aborted.");
                    }
                    else
                    {
                        player.dropMessage(5, "NEW World failed to be deployed. Check if needed ports are already in use or maximum world count has been reached.");
                    }
                }
            }
        });
    }
}
