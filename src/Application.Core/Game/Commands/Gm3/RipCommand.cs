using net.server;
using tools;

namespace Application.Core.Game.Commands.Gm3;

public class RipCommand : CommandBase
{
    public RipCommand() : base(3, "rip")
    {
        Description = "Send a RIP notice.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        Server.getInstance().broadcastMessage(c.getWorld(), PacketCreator.serverNotice(6, "[RIP]: " + joinStringFrom(paramsValue, 1)));
    }
}
