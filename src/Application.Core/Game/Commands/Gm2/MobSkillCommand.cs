using server.life;

namespace Application.Core.Game.Commands.Gm2;
public class MobSkillCommand : CommandBase
{
    public MobSkillCommand() : base(2, "mobskill")
    {
        Description = "Apply a mob skill to all mobs on the map. Args: <mob skill id> <skill level>";
    }

    public override void Execute(IClient client, string[] paramsValue)
    {
        if (paramsValue.Length < 2)
        {
            client.OnlinedCharacter.yellowMessage("Syntax: !mobskill <skillId> <skillLevel>");
            return;
        }

        if (!int.TryParse(paramsValue[0], out var skillId) || !int.TryParse(paramsValue[1], out var skillLevel))
            return;

        var possibleType = MobSkillTypeUtils.from(skillId);
        var possibleSkill = MobSkillFactory.getMobSkillOrThrow(possibleType, skillLevel);
        if (possibleSkill == null)
        {
            return;
        }

        var chr = client.OnlinedCharacter;
        MobSkill mobSkill = possibleSkill;
        chr.getMap().getAllMonsters().ForEach(
                monster => mobSkill.applyEffect(chr, monster, false, [])
        );
    }
}
