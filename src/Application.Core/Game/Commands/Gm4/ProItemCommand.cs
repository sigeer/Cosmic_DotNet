using Application.Core.Managers;
using client.inventory;
using client.inventory.manipulator;
using constants.inventory;
using server;

namespace Application.Core.Game.Commands.Gm4;

public class ProItemCommand : CommandBase
{
    public ProItemCommand() : base(4, "proitem")
    {
        Description = "Spawn an item with custom stats.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 2)
        {
            player.yellowMessage("Syntax: !proitem <itemid> <stat value> [<spdjmp value>]");
            return;
        }


        if (!int.TryParse(paramsValue[0], out var itemId))
        {
            player.yellowMessage("Syntax: itemId invalid");
            return;
        }

        ItemInformationProvider ii = ItemInformationProvider.getInstance();
        if (ii.getName(itemId) == null)
        {
            player.yellowMessage("Item id '" + paramsValue[0] + "' does not exist.");
            return;
        }

        if (!short.TryParse(paramsValue[1], out short stat))
        {
            player.yellowMessage("Invalid stat value.");
            return;
        }

        stat = Math.Max((short)0, stat);
        short spdjmp = 0;

        if (paramsValue.Length >= 3 && short.TryParse(paramsValue[2], out spdjmp))
            spdjmp = Math.Max((short)0, spdjmp);

        InventoryType type = ItemConstants.getInventoryType(itemId);
        if (type.Equals(InventoryType.EQUIP))
        {
            Item it = ii.getEquipById(itemId);
            it.setOwner(player.getName());

            ItemManager.SetEquipStat((Equip)it, stat, spdjmp);
            InventoryManipulator.addFromDrop(c, it);
        }
        else
        {
            player.dropMessage(6, "Make sure it's an equippable item.");
        }
    }
}
