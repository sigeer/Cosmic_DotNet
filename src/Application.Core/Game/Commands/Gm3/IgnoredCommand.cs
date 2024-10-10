using Application.Core.Managers;
using client.autoban;

namespace Application.Core.Game.Commands.Gm3;

public class IgnoredCommand : CommandBase
{
    public IgnoredCommand() : base(3, "ignored")
    {
        Description = "Show all characters being ignored in auto-ban alerts.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        foreach (int chrId in AutobanFactory.getIgnoredChrIds())
        {
            player.yellowMessage(CharacterManager.getNameById(chrId) + " is being ignored.");
        }
    }
}
