using client;

namespace Application.Core.Game.Commands.Gm2;

public class BuffMapCommand : CommandBase
{
    public BuffMapCommand() : base(2, "buffmap")
    {
        Description = "Give GM buffs to the whole map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        SkillFactory.GetSkillTrust(9101001).getEffect(SkillFactory.GetSkillTrust(9101001).getMaxLevel()).applyTo(player, true);
        SkillFactory.GetSkillTrust(9101002).getEffect(SkillFactory.GetSkillTrust(9101002).getMaxLevel()).applyTo(player, true);
        SkillFactory.GetSkillTrust(9101003).getEffect(SkillFactory.GetSkillTrust(9101003).getMaxLevel()).applyTo(player, true);
        SkillFactory.GetSkillTrust(9101008).getEffect(SkillFactory.GetSkillTrust(9101008).getMaxLevel()).applyTo(player, true);
        SkillFactory.GetSkillTrust(1005).getEffect(SkillFactory.GetSkillTrust(1005).getMaxLevel()).applyTo(player, true);

    }
}
