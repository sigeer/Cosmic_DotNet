using client.inventory;
using client.inventory.manipulator;

namespace Application.Core.Game.Commands.Gm2;

public class ClearSlotCommand : CommandBase
{
    public ClearSlotCommand() : base(2, "clearslot")
    {
        Description = "Clear all items in an inventory tab.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !clearslot <all, equip, use, setup, etc or cash.>");
            return;
        }

        string type = paramsValue[0];
        switch (type)
        {
            case "all":
                RemovePlayerSlot(c, InventoryType.EQUIP);
                RemovePlayerSlot(c, InventoryType.USE);
                RemovePlayerSlot(c, InventoryType.ETC);
                RemovePlayerSlot(c, InventoryType.SETUP);
                RemovePlayerSlot(c, InventoryType.CASH);
                player.yellowMessage("All Slots Cleared.");
                break;
            case "equip":
                RemovePlayerSlot(c, InventoryType.EQUIP);
                player.yellowMessage("Equipment Slot Cleared.");
                break;
            case "use":
                RemovePlayerSlot(c, InventoryType.USE);
                player.yellowMessage("Use Slot Cleared.");
                break;
            case "setup":
                RemovePlayerSlot(c, InventoryType.SETUP);
                player.yellowMessage("Set-Up Slot Cleared.");
                break;
            case "etc":
                RemovePlayerSlot(c, InventoryType.ETC);
                player.yellowMessage("ETC Slot Cleared.");
                break;
            case "cash":
                RemovePlayerSlot(c, InventoryType.CASH);
                player.yellowMessage("Cash Slot Cleared.");
                break;
            default:
                player.yellowMessage("Slot" + type + " does not exist!");
                break;
        }
    }

    private void RemovePlayerSlot(IClient c, InventoryType type)
    {
        for (int i = 0; i < 101; i++)
        {
            var tempItem = c.OnlinedCharacter.getInventory(type).getItem((byte)i);
            if (tempItem == null)
            {
                continue;
            }
            InventoryManipulator.removeFromSlot(c, InventoryType.CASH, (byte)i, tempItem.getQuantity(), false, false);
        }
    }
}
