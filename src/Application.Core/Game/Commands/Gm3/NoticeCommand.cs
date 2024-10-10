using net.server;
using tools;

namespace Application.Core.Game.Commands.Gm3;

public class NoticeCommand : CommandBase
{
    public NoticeCommand() : base(3, "notice")
    {
        Description = "Send a blue message to everyone on the server.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        Server.getInstance().broadcastMessage(c.getWorld(), PacketCreator.serverNotice(6, "[Notice] " + player.getLastCommandMessage()));
    }
}
