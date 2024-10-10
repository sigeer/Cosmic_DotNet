using client;

namespace Application.Core.Game.Commands.Gm2;
public class UnHideCommand : CommandBase
{
    public UnHideCommand() : base(2, "unhide")
    {
        Description = "Toggle Hide.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        SkillFactory.GetSkillTrust(9101004).getEffect(SkillFactory.GetSkillTrust(9101004).getMaxLevel()).applyTo(player);

    }
}
