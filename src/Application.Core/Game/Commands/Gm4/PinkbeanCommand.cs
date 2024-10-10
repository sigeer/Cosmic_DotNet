using constants.id;
using server.life;

namespace Application.Core.Game.Commands.Gm4;

public class PinkbeanCommand : CommandBase
{
    public PinkbeanCommand() : base(4, "pinkbean")
    {
        Description = "Spawn Pink Bean on your location.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        player.getMap().spawnMonsterOnGroundBelow(LifeFactory.getMonster(MobId.PINK_BEAN), player.getPosition());

    }
}
