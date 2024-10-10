using client;

namespace Application.Core.Game.Commands.Gm2;

public class MaxStatCommand : CommandBase
{
    public MaxStatCommand() : base(2, "maxstat")
    {
        Description = "Max out all character stats.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        player.loseExp(player.getExp(), false, false);
        player.setLevel(255);
        player.resetPlayerRates();
        if (YamlConfig.config.server.USE_ADD_RATES_BY_LEVEL)
        {
            player.setPlayerRates();
        }
        player.setWorldRates();
        player.updateStrDexIntLuk(short.MaxValue);
        player.setFame(13337);
        player.updateMaxHpMaxMp(30000, 30000);
        player.updateSingleStat(Stat.LEVEL, 255);
        player.updateSingleStat(Stat.FAME, 13337);
        player.yellowMessage("Stats maxed out.");
    }
}
