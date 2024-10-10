using Application.Core.Game.Life;
using Application.Core.Game.Maps;
using constants.id;
using net.server;
using server;
using server.maps;

namespace Application.Core.Game.Commands.Gm5;


public class DebugCommand : CommandBase
{
    private static string[] debugTypes = { "monster", "packet", "portal", "spawnpoint", "pos", "map", "mobsp", "event", "areas", "reactors", "servercoupons", "playercoupons", "timer", "marriage", "buff", "" };

    public DebugCommand() : base(5, "debug")
    {
        Description = "Show a debug message.";
    }


    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !debug <type>");
            return;
        }

        switch (paramsValue[0])
        {
            case "type":
            case "help":
                string msgTypes = "Available #bdebug types#k:\r\n\r\n";
                for (int i = 0; i < debugTypes.Length; i++)
                {
                    msgTypes += ("#L" + i + "#" + debugTypes[i] + "#l\r\n");
                }

                c.getAbstractPlayerInteraction().npcTalk(NpcId.STEWARD, msgTypes);
                break;

            case "monster":
                List<IMapObject> monsters = player.getMap().getMapObjectsInRange(player.getPosition(), double.PositiveInfinity, Arrays.asList(MapObjectType.MONSTER));
                foreach (var monstermo in monsters)
                {
                    var monster = (Monster)monstermo;
                    var controller = monster.getController();
                    player.message("Monster ID: " + monster.getId() + " Aggro target: " + ((controller != null) ? controller.getName() + " Has aggro: " + monster.isControllerHasAggro() + " Knowns aggro: " + monster.isControllerKnowsAboutAggro() : "<none>"));
                }
                break;

            case "packet":
                //player.getMap().broadcastMessage(PacketCreator.customPacket(joinStringFrom(params, 1)));
                break;

            case "portal":
                var portal = player.getMap().findClosestPortal(player.getPosition());
                if (portal != null)
                {
                    player.dropMessage(6, "Closest portal: " + portal.getId() + " '" + portal.getName() + "' Type: " + portal.getType() + " --> toMap: " + portal.getTargetMapId() + " scriptname: '" + portal.getScriptName() + "' state: " + (portal.getPortalState() ? 1 : 0) + ".");
                }
                else
                {
                    player.dropMessage(6, "There is no portal on this map.");
                }
                break;

            case "spawnpoint":
                var sp = player.getMap().findClosestSpawnpoint(player.getPosition());
                if (sp != null)
                {
                    player.dropMessage(6, "Closest mob spawn point: " + " Position: x " + sp.getPosition().X + " y " + sp.getPosition().Y + " Spawns mobid: '" + sp.getMonsterId() + "' --> canSpawn: " + !sp.getDenySpawn() + " canSpawnRightNow: " + sp.shouldSpawn() + ".");
                }
                else
                {
                    player.dropMessage(6, "There is no mob spawn point on this map.");
                }
                break;

            case "pos":
                player.dropMessage(6, "Current map position: (" + player.getPosition().X + ", " + player.getPosition().Y + ").");
                break;

            case "map":
                player.dropMessage(6, "Current map id " + player.getMap().getId() + ", event: '" + (player.getMap().getEventInstance()?.getName() ?? "null") + "'; Players: " + player.getMap().getAllPlayers().Count + ", Mobs: " + player.getMap().countMonsters() + ", Reactors: " + player.getMap().countReactors() + ", Items: " + player.getMap().countItems() + ", Objects: " + player.getMap().getMapObjects().Count + ".");
                break;

            case "mobsp":
                player.getMap().reportMonsterSpawnPoints(player);
                break;

            case "event":
                if (player.getEventInstance() == null)
                {
                    player.dropMessage(6, "Player currently not in an event.");
                }
                else
                {
                    player.dropMessage(6, "Current event name: " + player.getEventInstance()!.getName() + ".");
                }
                break;

            case "areas":
                player.dropMessage(6, "Configured areas on map " + player.getMapId() + ":");

                byte index = 0;
                foreach (Rectangle rect in player.getMap().getAreas())
                {
                    player.dropMessage(6, "Id: " + index + " -> posX: " + rect.X + " posY: '" + rect.Y + "' dX: " + rect.Width + " dY: " + rect.Height + ".");
                    index++;
                }
                break;

            case "reactors":
                player.dropMessage(6, "Current reactor states on map " + player.getMapId() + ":");

                foreach (var mmo in player.getMap().getReactors())
                {
                    Reactor mr = (Reactor)mmo;
                    player.dropMessage(6, "Id: " + mr.getId() + " Oid: " + mr.getObjectId() + " name: '" + mr.getName() + "' -> Type: " + mr.getReactorType() + " State: " + mr.getState() + " Event State: " + mr.getEventState() + " Position: x " + mr.getPosition().X + " y " + mr.getPosition().Y + ".");
                }
                break;

            case "servercoupons":
            case "coupons":
                string s = "Currently active SERVER coupons: ";
                foreach (int i in Server.getInstance().getActiveCoupons())
                {
                    s += (i + " ");
                }

                player.dropMessage(6, s);
                break;

            case "playercoupons":
                string st = "Currently active PLAYER coupons: ";
                foreach (int i in player.getActiveCoupons())
                {
                    st += (i + " ");
                }

                player.dropMessage(6, st);
                break;

            case "timer":
                TimerManager tMan = TimerManager.getInstance();
                player.dropMessage(6, "Total Task: " + tMan.getTaskCount() + " Current Task: " + tMan.getQueuedTasks() + " Active Task: " + tMan.getActiveCount() + " Completed Task: " + tMan.getCompletedTaskCount());
                break;

            case "marriage":
                c.getChannelServer().debugMarriageStatus();
                break;

            case "buff":
                c.OnlinedCharacter.debugListAllBuffs();
                break;
        }
    }
}
