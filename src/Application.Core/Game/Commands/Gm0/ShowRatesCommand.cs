namespace Application.Core.Game.Commands.Gm0;

public class ShowRatesCommand : CommandBase
{
    public ShowRatesCommand(): base(0, "showrates")
    {
        Description = "Show all world/character rates.";
    }

    public override void Execute(IClient c, string[] paramsValue)
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
