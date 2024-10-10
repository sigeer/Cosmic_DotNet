namespace Application.Core.Game.Commands.Gm0;

public class ReadPointsCommand : CommandBase
{
    public ReadPointsCommand(): base(0, "points")
    {
        Description = "Show point total.";
    }

    public override void Execute(IClient client, string[] paramsValue)
    {

        var player = client.OnlinedCharacter;
        if (paramsValue.Length > 2)
        {
            player.yellowMessage("Syntax: @points (rp|vp|all)");
            return;
        }
        else if (paramsValue.Length == 0)
        {
            player.yellowMessage("RewardPoints: " + player.getRewardPoints() + " | "
                    + "VotePoints: " + player.getClient().getVotePoints());
            return;
        }

        switch (paramsValue[0])
        {
            case "rp":
                player.yellowMessage("RewardPoints: " + player.getRewardPoints());
                break;
            case "vp":
                player.yellowMessage("VotePoints: " + player.getClient().getVotePoints());
                break;
            default:
                player.yellowMessage("RewardPoints: " + player.getRewardPoints() + " | "
                        + "VotePoints: " + player.getClient().getVotePoints());
                break;
        }
    }
}