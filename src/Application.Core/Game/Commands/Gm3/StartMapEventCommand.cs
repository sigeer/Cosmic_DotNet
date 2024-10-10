namespace Application.Core.Game.Commands.Gm3;

public class StartMapEventCommand : CommandBase
{
    public StartMapEventCommand() : base(3, "startmapevent")
    {
        Description = "Start a \"classic\" event on current map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        c.OnlinedCharacter.getMap().startEvent(c.OnlinedCharacter);
    }
}
