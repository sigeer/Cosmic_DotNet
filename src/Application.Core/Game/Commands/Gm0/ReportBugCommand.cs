using Application.Core.Managers;
using net.server;
using tools;

namespace Application.Core.Game.Commands.Gm0;

public class ReportBugCommand : CommandBase
{
    public ReportBugCommand(): base(0, "bug", "reportbug")
    {
        Description = "Send in a bug report.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        if (paramsValue.Length < 1)
        {
            player.dropMessage(5, $"Message too short and not sent. Please do {Syntax[0]} <bug>");
            return;
        }
        string message = player.getLastCommandMessage();
        Server.getInstance().broadcastGMMessage(c.getWorld(), PacketCreator.sendYellowTip("[Bug]:" + CharacterManager.makeMapleReadable(player.getName()) + ": " + message));
        Server.getInstance().broadcastGMMessage(c.getWorld(), PacketCreator.serverNotice(1, message));
        log.Information("{CharacterName}: {LastCommand}", CharacterManager.makeMapleReadable(player.getName()), message);
        player.dropMessage(5, "Your bug '" + message + "' was submitted successfully to our developers. Thank you!");

    }
}
