using Application.Core.Managers;
using client.inventory;
using client.inventory.manipulator;
using constants.inventory;
using server;

namespace Application.Core.Game.Commands.Gm2;

public class ItemCommand : CommandBase
{
    public ItemCommand() : base(2, "item")
    {
        Description = "Spawn an item into your inventory.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !item <itemid> <quantity>");
            return;
        }

        int itemId = int.Parse(paramsValue[0]);
        ItemInformationProvider ii = ItemInformationProvider.getInstance();

        if (ii.getName(itemId) == null)
        {
            player.yellowMessage("Item id '" + paramsValue[0] + "' does not exist.");
            return;
        }

        short quantity = 1;
        if (paramsValue.Length >= 2)
        {
            quantity = short.Parse(paramsValue[1]);
        }

        if (YamlConfig.config.server.BLOCK_GENERATE_CASH_ITEM && ii.isCash(itemId))
        {
            player.yellowMessage("You cannot create a cash item with this command.");
            return;
        }

        if (ItemConstants.isPet(itemId))
        {
            if (paramsValue.Length >= 2)
            {   // thanks to istreety & TacoBell
                quantity = 1;
                long days = Math.Max(1, int.Parse(paramsValue[1]));
                long expiration = DateTimeOffset.Now.AddDays(days).ToUnixTimeMilliseconds();
                int petid = ItemManager.CreatePet(itemId);

                InventoryManipulator.addById(c, itemId, quantity, player.getName(), petid, expiration: expiration);
                return;
            }
            else
            {
                player.yellowMessage("Pet Syntax: !item <itemid> <expiration>");
                return;
            }
        }

        short flag = 0;
        if (player.gmLevel() < 3)
        {
            flag |= ItemConstants.ACCOUNT_SHARING;
            flag |= ItemConstants.UNTRADEABLE;
        }

        InventoryManipulator.addById(c, itemId, quantity, player.getName(), -1, flag, -1);
    }
}
