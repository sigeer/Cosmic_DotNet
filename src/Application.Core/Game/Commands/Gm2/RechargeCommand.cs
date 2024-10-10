using client.inventory;
using constants.inventory;
using server;

namespace Application.Core.Game.Commands.Gm2;

public class RechargeCommand : CommandBase
{
    public RechargeCommand() : base(2, "recharge")
    {
        Description = "Recharge and refill all USE items.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        ItemInformationProvider ii = ItemInformationProvider.getInstance();
        foreach (Item torecharge in c.OnlinedCharacter.getInventory(InventoryType.USE).list())
        {
            if (ItemConstants.isThrowingStar(torecharge.getItemId()))
            {
                torecharge.setQuantity(ii.getSlotMax(c, torecharge.getItemId()));
                c.OnlinedCharacter.forceUpdateItem(torecharge);
            }
            else if (ItemConstants.isArrow(torecharge.getItemId()))
            {
                torecharge.setQuantity(ii.getSlotMax(c, torecharge.getItemId()));
                c.OnlinedCharacter.forceUpdateItem(torecharge);
            }
            else if (ItemConstants.isBullet(torecharge.getItemId()))
            {
                torecharge.setQuantity(ii.getSlotMax(c, torecharge.getItemId()));
                c.OnlinedCharacter.forceUpdateItem(torecharge);
            }
            else if (ItemConstants.isConsumable(torecharge.getItemId()))
            {
                torecharge.setQuantity(ii.getSlotMax(c, torecharge.getItemId()));
                c.OnlinedCharacter.forceUpdateItem(torecharge);
            }
        }
        player.dropMessage(5, "USE Recharged.");
    }
}
