using Application.Core.Managers;
using constants.id;
using server.maps;
using System.Text;

namespace Application.Core.Game.Commands.Gm2;

public class WarpCommand : ParamsCommandBase
{
    public WarpCommand() : base(["<mapid>"], 2, "warp")
    {
        Description = "Warp to a map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        try
        {
            if (!int.TryParse(paramsValue[0], out var mapId))
            {
                var findResult = ResManager.FindMapIdByName(paramsValue[0]);
                if (findResult.BestMatch != null)
                {
                    mapId = findResult.BestMatch.Id;
                }
                else if (findResult.MatchedItems.Count > 0)
                {
                    var messages = new StringBuilder("找到了这些相似项：");
                    foreach (var item in findResult.MatchedItems)
                    {
                        messages.Append($"\r\n{item.Id} - {item.StreetName} - {item.Name}");
                    }
                    c.getAbstractPlayerInteraction().npcTalk(NpcId.MAPLE_ADMINISTRATOR, messages.ToString());
                    return;
                }
                else
                {
                    player.yellowMessage("Map ID " + paramsValue[0] + " is invalid.");
                    return;
                }
            }

            var target = c.getChannelServer().getMapFactory().getMap(mapId);
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
