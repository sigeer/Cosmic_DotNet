namespace Application.Core.Game.Commands.Gm0;

public class LeaveEventCommand : CommandBase
{
    public LeaveEventCommand(): base(0, "leaveevent")
    {
        Description = "Leave active event.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        int returnMap = player.getSavedLocation("EVENT");
        if (returnMap != -1)
        {
            if (player.Ola != null)
            {
                player.Ola.resetTimes();
                player.Ola = null;
            }
            if (player.Fitness != null)
            {
                player.Fitness.resetTimes();
                player.Fitness = null;
            }

            player.saveLocationOnWarp();
            player.changeMap(returnMap);
            c.getChannelServer().getEvent()?.addLimit();
        }
        else
        {
            player.dropMessage(5, "You are not currently in an event.");
        }

    }
}
