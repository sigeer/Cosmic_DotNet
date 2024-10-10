using constants.id;

namespace Application.Core.Game.Commands.Gm0;

public class HelpCommand : CommandBase
{
    public HelpCommand(): base(0, "help", "commands")
    {
        Description = "Show available commands.";
    }

    public override void Execute(IClient client, string[] paramsValue)
    {
        client.getAbstractPlayerInteraction().openNpc(NpcId.STEWARD, "commands");
    }
}
