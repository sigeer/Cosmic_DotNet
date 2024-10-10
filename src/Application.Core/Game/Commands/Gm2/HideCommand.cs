using client;

namespace Application.Core.Game.Commands.Gm2;

public class HideCommand : CommandBase  
{
    public HideCommand() : base(2, "hide")
    {
        Description = "Hide from players.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        SkillFactory.GetSkillTrust(9101004).getEffect(SkillFactory.GetSkillTrust(9101004).getMaxLevel()).applyTo(player);

    }
}
