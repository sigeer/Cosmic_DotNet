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

public class RatesCommand : Command
{
    public RatesCommand()
    {
        setDescription("Show your rates.");
    }

    public override void execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        // travel rates not applicable since it's intrinsically a server/environment rate rather than a character rate
        string showMsg_ = "#eCHARACTER RATES#n" + "\r\n\r\n";
        showMsg_ += "EXP Rate: #e#b" + player.getExpRate() + "x#k#n" + (player.hasNoviceExpRate() ? " - novice rate" : "") + "\r\n";
        showMsg_ += "MESO Rate: #e#b" + player.getMesoRate() + "x#k#n" + "\r\n";
        showMsg_ += "DROP Rate: #e#b" + player.getDropRate() + "x#k#n" + "\r\n";
        showMsg_ += "BOSS DROP Rate: #e#b" + player.getBossDropRate() + "x#k#n" + "\r\n";
        if (YamlConfig.config.server.USE_QUEST_RATE)
        {
            showMsg_ += "QUEST Rate: #e#b" + c.getWorldServer().QuestRate + "x#k#n" + "\r\n";
        }

        player.showHint(showMsg_, 300);
    }
}
