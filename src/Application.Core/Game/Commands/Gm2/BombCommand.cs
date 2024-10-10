using constants.id;
using net.server;
using server.life;
using tools;

namespace Application.Core.Game.Commands.Gm2;

public class BombCommand : CommandBase
{
    public BombCommand() : base(2, "bomb")
    {
        Description = "Bomb a player, dealing damage.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length > 0)
        {
            var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
            if (victim != null && victim.IsOnlined)
            {
                victim.getMap().spawnMonsterOnGroundBelow(LifeFactory.getMonster(MobId.ARPQ_BOMB), victim.getPosition());
                Server.getInstance().broadcastGMMessage(c.getWorld(), PacketCreator.serverNotice(5, player.getName() + " used !bomb on " + victim.getName()));
            }
            else
            {
                player.message("Player '" + paramsValue[0] + "' could not be found on this world.");
            }
        }
        else
        {
            player.getMap().spawnMonsterOnGroundBelow(LifeFactory.getMonster(MobId.ARPQ_BOMB), player.getPosition());
        }
    }
}
