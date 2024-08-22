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
   @Author: MedicOP - Add clock commands
*/


using tools;

namespace client.command.commands.gm3;

public class TimerMapCommand : Command
{
    public TimerMapCommand()
    {
        setDescription("Set timer on all players in current map.");
    }

    public override void execute(Client c, string[] paramsValue)
    {
        Character player = c.getPlayer();
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !timermap <seconds>|remove");
            return;
        }

        if (paramsValue[0].Equals("remove", StringComparison.OrdinalIgnoreCase))
        {
            foreach (Character victim in player.getMap().getCharacters())
            {
                victim.sendPacket(PacketCreator.removeClock());
            }
        }
        else
        {
            try
            {
                int seconds = int.Parse(paramsValue[0]);
                foreach (Character victim in player.getMap().getCharacters())
                {
                    victim.sendPacket(PacketCreator.getClock(seconds));
                }
            }
            catch (FormatException e)
            {
                player.yellowMessage("Syntax: !timermap <seconds>|remove");
            }
        }
    }
}
