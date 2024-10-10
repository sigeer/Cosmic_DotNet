using Application.Core.Managers;
using net.packet.logging;

namespace Application.Core.Game.Commands.Gm3;
public class MonitorsCommand : CommandBase
{
    public MonitorsCommand() : base(3, "monitors")
    {
        Description = "Show all characters being monitored for packet logging";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        foreach (int chrId in MonitoredChrLogger.getMonitoredChrIds())
        {
            player.yellowMessage(CharacterManager.getNameById(chrId) + " is being monitored.");
        }
    }
}
