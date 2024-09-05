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

public class ServerAddChannelCommand : Command
{
    public ServerAddChannelCommand()
    {
        setDescription("Add a new channel to a world.");
    }

    public override void execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        if (paramsValue.Length < 1)
        {
            player.dropMessage(5, "Syntax: @addchannel <worldid>");
            return;
        }

        int worldid = int.Parse(paramsValue[0]);

        ThreadManager.getInstance().newTask(() =>
        {
            int chid = Server.getInstance().addChannel(worldid);
            if (player.isLoggedinWorld())
            {
                if (chid >= 0)
                {
                    player.dropMessage(5, "NEW Channel " + chid + " successfully deployed on world " + worldid + ".");
                }
                else
                {
                    if (chid == -3)
                    {
                        player.dropMessage(5, "Invalid worldid detected. Channel creation aborted.");
                    }
                    else if (chid == -2)
                    {
                        player.dropMessage(5, "Reached channel limit on worldid " + worldid + ". Channel creation aborted.");
                    }
                    else if (chid == -1)
                    {
                        player.dropMessage(5, "Error detected when loading the 'world.ini' file. Channel creation aborted.");
                    }
                    else
                    {
                        player.dropMessage(5, "NEW Channel failed to be deployed. Check if the needed port is already in use or other limitations are taking place.");
                    }
                }
            }
        });
    }
}
