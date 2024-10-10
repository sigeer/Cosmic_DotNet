using constants.id;
using server.life;

namespace Application.Core.Game.Commands.Gm4;

public class ZakumCommand : CommandBase
{
    public ZakumCommand() : base(4, "zakum")
    {
        Description = "Spawn Zakum on your location.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        player.getMap().spawnFakeMonsterOnGroundBelow(LifeFactory.getMonster(MobId.ZAKUM_1), player.getPosition());
        for (int mobId = MobId.ZAKUM_ARM_1; mobId <= MobId.ZAKUM_ARM_8; mobId++)
        {
            player.getMap().spawnMonsterOnGroundBelow(LifeFactory.getMonster(mobId), player.getPosition());
        }
    }
}
