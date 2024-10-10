using server.life;

namespace Application.Core.Game.Commands.Gm3;

public class SpawnCommand : CommandBase
{
    public SpawnCommand() : base(3, "spawn")
    {
        Description = "Spawn mob(s) on your location.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !spawn <mobid> [<mobqty>]");
            return;
        }

        var monster = LifeFactory.getMonster(int.Parse(paramsValue[0]));
        if (monster == null)
        {
            return;
        }
        if (paramsValue.Length == 2)
        {
            for (int i = 0; i < int.Parse(paramsValue[1]); i++)
            {
                player.getMap().spawnMonsterOnGroundBelow(monster, player.getPosition());
            }
        }
        else
        {
            player.getMap().spawnMonsterOnGroundBelow(monster, player.getPosition());
        }
    }
}
