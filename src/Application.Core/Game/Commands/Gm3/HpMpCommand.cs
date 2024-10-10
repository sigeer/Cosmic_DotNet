namespace Application.Core.Game.Commands.Gm3;

public class HpMpCommand : CommandBase
{
    public HpMpCommand() : base(3, "hpmp")
    {
        Description = "Set HP/MP of a player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        var victim = player;
        int statUpdate = 1;

        if (paramsValue.Length == 2)
        {
            victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
            statUpdate = int.Parse(paramsValue[1]);
        }
        else if (paramsValue.Length == 1)
        {
            statUpdate = int.Parse(paramsValue[0]);
        }
        else
        {
            player.yellowMessage("Syntax: !hpmp [<playername>] <value>");
        }

        if (victim != null && victim.IsOnlined)
        {
            victim.updateHpMp(statUpdate);
        }
        else
        {
            player.message("Player '" + paramsValue[0] + "' could not be found on this world.");
        }
    }
}
