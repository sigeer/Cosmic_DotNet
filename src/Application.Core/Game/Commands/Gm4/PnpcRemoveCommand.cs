using Application.Core.constants.game;

namespace Application.Core.Game.Commands.Gm4;

public class PnpcRemoveCommand : CommandBase
{
    public PnpcRemoveCommand() : base(4, "pnpcremove")
    {
        Description = "Remove a permanent NPC on the map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        int mapId = player.getMapId();
        int npcId = paramsValue.Length > 0 ? int.Parse(paramsValue[0]) : -1;

        Point pos = player.getPosition();
        int xpos = pos.X;
        int ypos = pos.Y;

        try
        {
            using var dbContext = new DBContext();
            var preSearch = dbContext.Plives.Where(x => x.World == player.getWorld() && x.Map == mapId && x.Type == LifeType.NPC);

            if (npcId > -1)
            {
                preSearch = preSearch.Where(x => x.Life == npcId);
            }
            else
            {
                preSearch = preSearch.Where(x => x.X >= xpos - 50 && x.X <= xpos + 50 && x.Y >= ypos - 50 && x.Y <= ypos + 50);
            }

            var dataList = preSearch.ToList();
            dbContext.Plives.RemoveRange(dataList);
            var toRemove = dataList.Select(x => new { x.Life, x.X, x.Y }).ToList();


            if (toRemove.Count > 0)
            {
                foreach (var ch in player.getWorldServer().getChannels())
                {
                    var map = ch.getMapFactory().getMap(mapId);

                    foreach (var r in toRemove)
                    {
                        map.destroyNPC(r.Life);
                    }
                }
            }

            player.yellowMessage("Cleared " + toRemove.Count + " pNPC placements.");
        }
        catch (Exception e)
        {
            log.Error(e.ToString());
            player.dropMessage(5, "Failed to remove pNPC from the database.");
        }
    }
}