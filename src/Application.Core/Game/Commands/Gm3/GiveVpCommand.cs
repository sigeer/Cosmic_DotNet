namespace Application.Core.Game.Commands.Gm3;

public class GiveVpCommand : CommandBase
{
    public GiveVpCommand() : base(3, "givevp")
    {
        Description = "Give vote points to a player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 2)
        {
            player.yellowMessage("Syntax: !givevp <playername> <gainvotepoint>");
            return;
        }

        var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (victim != null && victim.IsOnlined)
        {
            victim.getClient().addVotePoints(int.Parse(paramsValue[1]));
            player.message("VP given.");
        }
        else
        {
            player.message("Player '" + paramsValue[0] + "' could not be found.");
        }
    }
}
