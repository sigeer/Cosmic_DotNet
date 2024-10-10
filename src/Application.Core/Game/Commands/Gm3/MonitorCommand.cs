using net.packet.logging;
using net.server;
using tools;

namespace Application.Core.Game.Commands.Gm3;

public class MonitorCommand : CommandBase
{
    public MonitorCommand() : base(3, "monitor")
    {
        Description = "Toggle monitored packet logging of a character.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !monitor <ign>");
            return;
        }
        var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (victim == null || !victim.IsOnlined)
        {
            player.message("Player '" + paramsValue[0] + "' could not be found on this world.");
            return;
        }
        bool monitored = MonitoredChrLogger.toggleMonitored(victim.getId());
        player.yellowMessage(victim.getId() + " is " + (monitored ? "now being monitored." : "no longer being monitored."));
        string message = player.getName() + (monitored ? " has started monitoring " : " has stopped monitoring ") + victim.getId() + ".";
        Server.getInstance().broadcastGMMessage(c.getWorld(), PacketCreator.serverNotice(5, message));

    }
}
