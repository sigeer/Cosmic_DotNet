namespace Application.Core.Game.Commands.Gm3;
public class GiveRpCommand : CommandBase
{
    public GiveRpCommand() : base(3, "giverp")
    {
        Description = "Give reward points to a player.";
    }

    public override void Execute(IClient client, string[] paramsValue)
    {
        var player = client.OnlinedCharacter;
        if (paramsValue.Length < 2)
        {
            player.yellowMessage("Syntax: !giverp <playername> <gainrewardpoint>");
            return;
        }

        var victim = client.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (victim != null && victim.IsOnlined)
        {
            victim.setRewardPoints(victim.getRewardPoints() + int.Parse(paramsValue[1]));
            player.message("RP given. Player " + paramsValue[0] + " now has " + victim.getRewardPoints()
                    + " reward points.");
        }
        else
        {
            player.message("Player '" + paramsValue[0] + "' could not be found.");
        }
    }
}
