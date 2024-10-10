using constants.game;
using constants.id;
using server.maps;

namespace Application.Core.Game.Commands.Gm1;



public class GotoCommand : CommandBase
{
    public GotoCommand() : base(1, "goto")
    {
        Description = "Warp to a predefined map.";

        var towns = GameConstants.GOTO_TOWNS.ToList();
        sortGotoEntries(towns);

        try
        {
            // thanks shavit for noticing goto areas getting loaded from wz needlessly only for the name retrieval

            foreach (var e in towns)
            {
                GOTO_TOWNS_INFO += ("'" + e.Key + "' - #b" + (MapFactory.loadPlaceName(e.Value)) + "#k\r\n");
            }

            List<KeyValuePair<string, int>> areas = new(GameConstants.GOTO_AREAS);
            sortGotoEntries(areas);
            foreach (var e in areas)
            {
                GOTO_AREAS_INFO += ("'" + e.Key + "' - #b" + (MapFactory.loadPlaceName(e.Value)) + "#k\r\n");
            }
        }
        catch (Exception e)
        {
            log.Error(e.ToString());

            GOTO_TOWNS_INFO = "(none)";
            GOTO_AREAS_INFO = "(none)";
        }

    }

    public static string GOTO_TOWNS_INFO = "";
    public static string GOTO_AREAS_INFO = "";

    private static void sortGotoEntries(List<KeyValuePair<string, int>> listEntries)
    {
        listEntries.Sort((e1, e2) => e1.Value.CompareTo(e2.Value));
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            string sendStr = "Syntax: #b@goto <map name>#k. Available areas:\r\n\r\n#rTowns:#k\r\n" + GOTO_TOWNS_INFO;
            if (player.isGM())
            {
                sendStr += ("\r\n#rAreas:#k\r\n" + GOTO_AREAS_INFO);
            }

            player.getAbstractPlayerInteraction().npcTalk(NpcId.SPINEL, sendStr);
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
                player.dropMessage(1, "This command can not be used in this map.");
                return;
            }
        }

        Dictionary<string, int> gotomaps;
        if (player.isGM())
        {
            gotomaps = new(GameConstants.GOTO_AREAS);     // distinct map registry for GM/users suggested thanks to Vcoc
            gotomaps.putAll(GameConstants.GOTO_TOWNS);  // thanks Halcyon (UltimateMors) for pointing out duplicates on listed entries functionality
        }
        else
        {
            gotomaps = GameConstants.GOTO_TOWNS;
        }

        if (gotomaps.ContainsKey(paramsValue[0]))
        {
            var target = c.getChannelServer().getMapFactory().getMap(gotomaps[paramsValue[0]]);

            // expedition issue with this command detected thanks to Masterrulax
            Portal targetPortal = target.getRandomPlayerSpawnpoint();
            player.saveLocationOnWarp();
            player.changeMap(target, targetPortal);
        }
        else
        {
            // detailed info on goto available areas suggested thanks to Vcoc
            string sendStr = "Area '#r" + paramsValue[0] + "#k' is not available. Available areas:\r\n\r\n#rTowns:#k" + GOTO_TOWNS_INFO;
            if (player.isGM())
            {
                sendStr += ("\r\n#rAreas:#k\r\n" + GOTO_AREAS_INFO);
            }

            player.getAbstractPlayerInteraction().npcTalk(NpcId.SPINEL, sendStr);
        }
    }
}
