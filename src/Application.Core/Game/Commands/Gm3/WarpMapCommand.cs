using Application.Core.Managers;
using constants.id;
using System.Collections.Generic;
using System.Text;

namespace Application.Core.Game.Commands.Gm3;

public class WarpMapCommand : CommandBase
{
    public WarpMapCommand() : base(3, "warpmap")
    {
        Description = "Warp all characters on current map to a new map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !warpmap <mapid>");
            return;
        }

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
            var characters = player.getMap().getAllPlayers();

            foreach (var victim in characters)
            {
                victim.saveLocationOnWarp();
                victim.changeMap(target, target.getRandomPlayerSpawnpoint());
            }
        }
        catch (Exception ex)
        {
            log.Warning(ex.ToString());
            player.yellowMessage("Map ID " + paramsValue[0] + " is invalid.");
        }
    }
}
