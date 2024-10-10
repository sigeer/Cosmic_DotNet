namespace Application.Core.Game.Commands.Gm0;

public class RatesCommand : CommandBase
{
    public RatesCommand() : base(0, "rates")
    {
        Description = "Show your rates.";
    }

    public override void Execute(IClient c, string[] paramsValue)
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
