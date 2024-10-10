using constants.id;
using server.life;

namespace Application.Core.Game.Commands.Gm4;

public class PianusCommand : CommandBase
{
    public PianusCommand() : base(4, "pianus")
    {
        Description = "Spawn Pianus (R) on your location.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        player.getMap().spawnMonsterOnGroundBelow(LifeFactory.getMonster(MobId.PIANUS_R), player.getPosition());
    }
}
