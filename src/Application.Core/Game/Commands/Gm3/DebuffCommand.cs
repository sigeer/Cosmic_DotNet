using client;
using server.life;
using server.maps;

namespace Application.Core.Game.Commands.Gm3;
public class DebuffCommand : CommandBase
{
    public DebuffCommand() : base(3, "debuff")
    {
        Description = "Put a debuff on all nearby players.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !debuff SLOW|SEDUCE|ZOMBIFY|CONFUSE|STUN|POISON|SEAL|DARKNESS|WEAKEN|CURSE");
            return;
        }

        Disease? disease = null;
        MobSkill? skill = null;

        switch (paramsValue[0].ToUpper())
        {
            case "SLOW":
                {
                    disease = Disease.SLOW;
                    skill = MobSkillFactory.getMobSkill(MobSkillType.SLOW, 7);
                    break;
                }
            case "SEDUCE":
                {
                    disease = Disease.SEDUCE;
                    skill = MobSkillFactory.getMobSkill(MobSkillType.SEDUCE, 7);
                    break;
                }
            case "ZOMBIFY":
                {
                    disease = Disease.ZOMBIFY;
                    skill = MobSkillFactory.getMobSkill(MobSkillType.UNDEAD, 1); break;
                }
            case "CONFUSE":
                {
                    disease = Disease.CONFUSE;
                    skill = MobSkillFactory.getMobSkill(MobSkillType.REVERSE_INPUT, 2); break;
                }
            case "STUN":
                {
                    disease = Disease.STUN;
                    skill = MobSkillFactory.getMobSkill(MobSkillType.STUN, 7); break;
                }
            case "POISON":
                {
                    disease = Disease.POISON;
                    skill = MobSkillFactory.getMobSkill(MobSkillType.POISON, 5); break;
                }
            case "SEAL":
                {
                    disease = Disease.SEAL;
                    skill = MobSkillFactory.getMobSkill(MobSkillType.SEAL, 1); break;
                }
            case "DARKNESS":
                {
                    disease = Disease.DARKNESS;
                    skill = MobSkillFactory.getMobSkill(MobSkillType.DARKNESS, 1); break;
                }
            case "WEAKEN":
                {
                    disease = Disease.WEAKEN;
                    skill = MobSkillFactory.getMobSkill(MobSkillType.WEAKNESS, 1); break;
                }
            case "CURSE":
                {
                    disease = Disease.CURSE;
                    skill = MobSkillFactory.getMobSkill(MobSkillType.CURSE, 1); break;
                }
        }

        if (disease == null || skill == null)
        {
            player.yellowMessage("Syntax: !debuff SLOW|SEDUCE|ZOMBIFY|CONFUSE|STUN|POISON|SEAL|DARKNESS|WEAKEN|CURSE");
            return;
        }

        foreach (var mmo in player.getMap().getMapObjectsInRange(player.getPosition(), 777777.7, Arrays.asList(MapObjectType.PLAYER)))
        {
            IPlayer chr = (IPlayer)mmo;

            if (chr.getId() != player.getId())
            {
                chr.giveDebuff(disease, skill);
            }
        }
    }
}
