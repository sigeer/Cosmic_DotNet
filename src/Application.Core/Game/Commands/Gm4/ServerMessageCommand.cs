namespace Application.Core.Game.Commands.Gm4;

public class ServerMessageCommand : CommandBase
{
    public ServerMessageCommand() : base(4, "servermessage")
    {
        Description = "Set scrolling server message.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        c.getWorldServer().setServerMessage(player.getLastCommandMessage());
    }
}
