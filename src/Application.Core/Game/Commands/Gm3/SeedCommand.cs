using client.inventory;
using constants.id;

namespace Application.Core.Game.Commands.Gm3;

public class SeedCommand : CommandBase
{
    public SeedCommand() : base(3, "seed")
    {
        Description = "Drop all seeds inside Henesys PQ.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (player.getMapId() != MapId.HENESYS_PQ)
        {
            player.yellowMessage("This command can only be used in HPQ.");
            return;
        }
        Point[] pos = { new Point(7, -207), new Point(179, -447), new Point(-3, -687), new Point(-357, -687), new Point(-538, -447), new Point(-359, -207) };
        int[] seed = {ItemId.PINK_PRIMROSE_SEED, ItemId.PURPLE_PRIMROSE_SEED, ItemId.GREEN_PRIMROSE_SEED,
                ItemId.BLUE_PRIMROSE_SEED, ItemId.YELLOW_PRIMROSE_SEED, ItemId.BROWN_PRIMROSE_SEED};
        for (int i = 0; i < pos.Length; i++)
        {
            Item item = new Item(seed[i], 0, 1);
            player.getMap().spawnItemDrop(player, player, item, pos[i], false, true);
            try
            {
                Thread.Sleep(100);
            }
            catch (ThreadInterruptedException e)
            {
                log.Error(e.ToString());
            }
        }
    }
}
