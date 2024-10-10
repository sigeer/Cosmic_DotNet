using constants.id;

namespace Application.Core.Game.Commands.Gm0;

public class StaffCommand : CommandBase
{
    public StaffCommand() : base(0, "credits")
    {
        Description = "Show credits. These people made the server possible.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        c.getAbstractPlayerInteraction().openNpc(NpcId.HERACLE, "credits");
    }
}
