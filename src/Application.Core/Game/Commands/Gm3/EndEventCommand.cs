namespace Application.Core.Game.Commands.Gm3;

public class EndEventCommand : CommandBase
{
    public EndEventCommand() : base(3, "endevent")
    {
        Description = "Close entry for ongoing event.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        c.getChannelServer().setEvent(null);
        player.dropMessage(5, "You have ended the event. No more players may join.");
    }
}
