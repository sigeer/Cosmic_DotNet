using client;

namespace Application.Core.Game.Commands.Gm2;

public class EmpowerMeCommand : CommandBase
{
    public EmpowerMeCommand() : base(2, "empowerme")
    {
        Description = "Activate all useful buffs.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        int[] array = { 2311003, 2301004, 1301007, 4101004, 2001002, 1101007, 1005, 2301003, 5121009, 1111002, 4111001, 4111002, 4211003, 4211005, 1321000, 2321004, 3121002 };
        foreach (int i in array)
        {
            var skill = SkillFactory.GetSkillTrust(i);
            skill.getEffect(skill.getMaxLevel()).applyTo(player);
        }
    }
}
