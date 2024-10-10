using client;
using provider;
using provider.wz;

namespace Application.Core.Game.Commands.Gm2;
public class ResetSkillCommand : CommandBase
{
    public ResetSkillCommand() : base(2, "resetskill")
    {
        Description = "Set all skill levels to 0.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        foreach (Data skill_ in DataProviderFactory.getDataProvider(WZFiles.STRING).getData("Skill.img").getChildren())
        {
            try
            {
                var skill = SkillFactory.GetSkillTrust(int.Parse(skill_.getName()!));
                player.changeSkillLevel(skill, 0, skill.getMaxLevel(), -1);
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                break;
            }
        }

        if (player.getJob().isA(Job.ARAN1) || player.getJob().isA(Job.LEGEND))
        {
            Skill skill = SkillFactory.GetSkillTrust(5001005);
            player.changeSkillLevel(skill, -1, -1, -1);
        }
        else
        {
            Skill skill = SkillFactory.GetSkillTrust(21001001);
            player.changeSkillLevel(skill, -1, -1, -1);
        }

        player.yellowMessage("Skills reseted.");
    }
}
