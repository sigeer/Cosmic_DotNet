using net.server;

namespace Application.Core.Game.Commands.Gm3;

public class ReloadEventsCommand : CommandBase
{
    public ReloadEventsCommand() : base(3, "reloadevents")
    {
        Description = "Reload all event data.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        foreach (var ch in Server.getInstance().getAllChannels())
        {
            ch.reloadEventScriptManager();
        }
        player.dropMessage(5, "Reloaded Events");
    }
}
