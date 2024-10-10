using constants.id;
using server.life;

namespace Application.Core.Game.Commands.Gm4;

public class PapCommand : CommandBase
{
    public PapCommand() : base(4, "pap")
    {
        Description = "Spawn Papulatus on your location.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        // thanks Conrad for noticing mobid typo here
        player.getMap().spawnMonsterOnGroundBelow(LifeFactory.getMonster(MobId.PAPULATUS_CLOCK), player.getPosition());
    }
}
