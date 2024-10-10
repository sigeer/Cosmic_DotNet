using client;

namespace Application.Core.Game.Commands.Gm3;

public class FameCommand : CommandBase
{
    public FameCommand() : base(3, "fame")
    {
        Description = "Set new fame value of a player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 2)
        {
            player.yellowMessage("Syntax: !fame <playername> <newfame>");
            return;
        }

        if (!int.TryParse(paramsValue[1], out var fame))
        {
            player.yellowMessage("Syntax: <newfame> invalid");
            return;
        }

        var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (victim != null && victim.IsOnlined)
        {
            victim.setFame(fame);
            victim.updateSingleStat(Stat.FAME, victim.getFame());
            player.message("FAME given.");
        }
        else
        {
            player.message("Player '" + paramsValue[0] + "' could not be found.");
        }
    }
}
