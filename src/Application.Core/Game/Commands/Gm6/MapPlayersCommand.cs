using net.server;

namespace Application.Core.Game.Commands.Gm6;

public class MapPlayersCommand : CommandBase
{
    public MapPlayersCommand() : base(6, "mapplayers")
    {
        Description = "Show all players on the map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        string names = "";
        int map = player.getMapId();
        foreach (var world in Server.getInstance().getWorlds())
        {
            foreach (var chr in world.getPlayerStorage().GetAllOnlinedPlayers())
            {
                int curMap = chr.getMapId();
                string hp = chr.getHp().ToString();
                string maxhp = chr.getCurrentMaxHp().ToString();
                string name = chr.getName() + ": " + hp + "/" + maxhp;
                if (map == curMap)
                {
                    names = names.Equals("") ? name : (names + ", " + name);
                }
            }
        }
        player.message("Players on mapid " + map + ": " + names);
    }
}
