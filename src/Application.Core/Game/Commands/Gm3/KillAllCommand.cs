using Application.Core.Game.Life;
using constants.id;
using server.maps;

namespace Application.Core.Game.Commands.Gm3;

public class KillAllCommand : CommandBase
{
    public KillAllCommand() : base(3, "killall")
    {
        Description = "Kill all mobs in the map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        var map = player.getMap();
        var monsters = map.getMapObjectsInRange(player.getPosition(), double.PositiveInfinity, Arrays.asList(MapObjectType.MONSTER));
        int count = 0;
        foreach (var monstermo in monsters)
        {
            Monster monster = (Monster)monstermo;
            if (!monster.getStats().isFriendly() && !(monster.getId() >= MobId.DEAD_HORNTAIL_MIN && monster.getId() <= MobId.HORNTAIL))
            {
                map.damageMonster(player, monster, int.MaxValue);
                count++;
            }
        }
        player.dropMessage(5, "Killed " + count + " monsters.");
    }
}
