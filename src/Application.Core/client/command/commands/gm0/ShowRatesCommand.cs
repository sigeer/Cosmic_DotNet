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

public class ShowRatesCommand : Command
{
    public ShowRatesCommand()
    {
        setDescription("Show all world/character rates.");
    }

    public override void execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        string showMsg = "#eEXP RATE#n" + "\r\n";
        showMsg += "World EXP Rate: #k" + c.getWorldServer().ExpRate + "x#k" + "\r\n";
        showMsg += "Player EXP Rate: #k" + player.getRawExpRate() + "x#k" + "\r\n";
        if (player.getCouponExpRate() != 1)
        {
            showMsg += "Coupon EXP Rate: #k" + player.getCouponExpRate() + "x#k" + "\r\n";
        }
        showMsg += "EXP Rate: #e#b" + player.getExpRate() + "x#k#n" + (player.hasNoviceExpRate() ? " - novice rate" : "") + "\r\n";

        showMsg += "\r\n" + "#eMESO RATE#n" + "\r\n";
        showMsg += "World MESO Rate: #k" + c.getWorldServer().MesoRate + "x#k" + "\r\n";
        showMsg += "Player MESO Rate: #k" + player.getRawMesoRate() + "x#k" + "\r\n";
        if (player.getCouponMesoRate() != 1)
        {
            showMsg += "Coupon MESO Rate: #k" + player.getCouponMesoRate() + "x#k" + "\r\n";
        }
        showMsg += "MESO Rate: #e#b" + player.getMesoRate() + "x#k#n" + "\r\n";

        showMsg += "\r\n" + "#eDROP RATE#n" + "\r\n";
        showMsg += "World DROP Rate: #k" + c.getWorldServer().DropRate + "x#k" + "\r\n";
        showMsg += "Player DROP Rate: #k" + player.getRawDropRate() + "x#k" + "\r\n";
        if (player.getCouponDropRate() != 1)
        {
            showMsg += "Coupon DROP Rate: #k" + player.getCouponDropRate() + "x#k" + "\r\n";
        }
        showMsg += "DROP Rate: #e#b" + player.getDropRate() + "x#k#n" + "\r\n";

        showMsg += "\r\n" + "#eBOSS DROP RATE#n" + "\r\n";
        showMsg += "World BOSS DROP Rate: #k" + c.getWorldServer().BossDropRate + "x#k" + "\r\n";
        showMsg += "Player DROP Rate: #k" + player.getRawDropRate() + "x#k" + "\r\n";
        if (player.getCouponDropRate() != 1)
        {
            showMsg += "Coupon DROP Rate: #k" + player.getCouponDropRate() + "x#k" + "\r\n";
        }
        showMsg += "BOSS DROP Rate: #e#b" + player.getBossDropRate() + "x#k#n" + "\r\n";

        if (YamlConfig.config.server.USE_QUEST_RATE)
        {
            showMsg += "\r\n" + "#eQUEST RATE#n" + "\r\n";
            showMsg += "World QUEST Rate: #e#b" + c.getWorldServer().QuestRate + "x#k#n" + "\r\n";
        }

        showMsg += "\r\n";
        showMsg += "World TRAVEL Rate: #e#b" + c.getWorldServer().TravelRate + "x#k#n" + "\r\n";

        player.showHint(showMsg, 300);
    }
}
