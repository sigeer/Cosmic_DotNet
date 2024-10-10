using server.maps;

namespace Application.Core.Game.Commands.Gm2;

public class WarpCommand : CommandBase
{
    public WarpCommand() : base(2, "warp")
    {
        Description = "Warp to a map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !warp <mapid>");
            return;
        }

        try
        {
            var target = c.getChannelServer().getMapFactory().getMap(int.Parse(paramsValue[0]));
            if (target == null)
            {
                player.yellowMessage("Map ID " + paramsValue[0] + " is invalid.");
                return;
            }

            if (!player.isAlive())
            {
                player.dropMessage(1, "This command cannot be used when you're dead.");
                return;
            }

            if (!player.isGM())
            {
                if (player.getEventInstance() != null || MiniDungeonInfo.isDungeonMap(player.getMapId()) || FieldLimit.CANNOTMIGRATE.check(player.getMap().getFieldLimit()))
                {
                    player.dropMessage(1, "This command cannot be used in this map.");
                    return;
                }
            }

            // expedition issue with this command detected thanks to Masterrulax
            player.saveLocationOnWarp();
            player.changeMap(target, target.getRandomPlayerSpawnpoint());
        }
        catch (Exception ex)
        {
            log.Warning(ex.ToString());
            player.yellowMessage("Map ID " + paramsValue[0] + " is invalid.");
        }
    }
}
