using client;
using provider;
using provider.wz;

namespace Application.Core.Game.Commands.Gm2;

public class MaxSkillCommand : CommandBase
{
    public MaxSkillCommand() : base(2, "maxskill")
    {
        Description = "Max out all job skills.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        foreach (Data skill_ in DataProviderFactory.getDataProvider(WZFiles.STRING).getData("Skill.img").getChildren())
        {
            try
            {
                Skill skill = SkillFactory.GetSkillTrust(int.Parse(skill_.getName()));
                player.changeSkillLevel(skill, (sbyte)skill.getMaxLevel(), skill.getMaxLevel(), -1);
            }
            catch (Exception nfe)
            {
                log.Error(nfe.ToString());
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

        player.yellowMessage("Skills maxed out.");
    }
}
