using client;

namespace Application.Core.Game.Commands.Gm2;

public class BuffCommand : CommandBase
{
    public BuffCommand() : base(2, "buff")
    {
        Description = "Activate a buff.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !buff <buffid>");
            return;
        }
        int skillid = int.Parse(paramsValue[0]);

        var skill = SkillFactory.getSkill(skillid);
        if (skill != null)
        {
            skill.getEffect(skill.getMaxLevel()).applyTo(player);
        }
    }
}
