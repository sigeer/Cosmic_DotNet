namespace Application.Core.Game.Commands.Gm3;

public class StopMapEventCommand : CommandBase
{
    public StopMapEventCommand() : base(3, "stopmapevent")
    {
        Description = "Stop ongoing \"classic\" event.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        c.OnlinedCharacter.getMap().setEventStarted(false);
    }
}
