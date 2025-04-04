using net.server;

namespace Application.Core.Game.Commands.Gm0;

public class TimeCommand : CommandBase
{
    public TimeCommand() : base(0, "time")
    {
        Description = "Show current server time.";
    }

    public override void Execute(IClient client, string[] paramsValue)
    {
        client.OnlinedCharacter.yellowMessage("Server Time: " + DateTimeOffset.FromUnixTimeMilliseconds(Server.getInstance().getCurrentTime()).ToString("yyyy-MM-dd HH:mm:ss"));
    }
}
