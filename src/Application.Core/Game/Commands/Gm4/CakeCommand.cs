using constants.id;
using server.life;

namespace Application.Core.Game.Commands.Gm4;

public class CakeCommand : CommandBase
{
    public CakeCommand() : base(4, "cake")
    {
        Description = "Spawn Cake boss with specified HP.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        var monster = LifeFactory.getMonster(MobId.GIANT_CAKE)!;
        if (paramsValue.Length == 1 && double.TryParse(paramsValue[0], out var mobHp))
        {
            int newHp = (mobHp <= 0) ? int.MaxValue : ((mobHp > int.MaxValue) ? int.MaxValue : (int)mobHp);

            monster.setStartingHp(newHp);
        }

        player.getMap().spawnMonsterOnGroundBelow(monster, player.getPosition());
    }
}
