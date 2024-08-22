

using client;
using client.inventory;
using client.inventory.manipulator;
using net.packet;
using server;
using server.quest;
using tools;

namespace net.server.channel.handlers;



/**
 * @author Xari
 * @author Ronan - added concurrency protection and quest progress limit
 */
public class RaiseIncExpHandler : AbstractPacketHandler
{

    public override void handlePacket(InPacket p, Client c)
    {
        sbyte inventorytype = p.ReadSByte();//nItemIT
        short slot = p.readShort();//nSlotPosition
        int itemid = p.readInt();//nItemID

        if (c.tryacquireClient())
        {
            try
            {
                ItemInformationProvider ii = ItemInformationProvider.getInstance();
                var consItem = ii.getQuestConsumablesInfo(itemid);
                if (consItem == null)
                {
                    return;
                }

                int infoNumber = consItem.questid;
                Dictionary<int, int> consumables = consItem.items;

                Character chr = c.getPlayer();
                Quest quest = Quest.getInstanceFromInfoNumber(infoNumber);
                if (!chr.getQuest(quest).getStatus().Equals(QuestStatus.Status.STARTED))
                {
                    c.sendPacket(PacketCreator.enableActions());
                    return;
                }

                int consId;
                Inventory inv = chr.getInventory(InventoryTypeUtils.getByType(inventorytype));
                inv.lockInventory();
                try
                {
                    consId = inv.getItem(slot).getItemId();
                    if (!consumables.ContainsKey(consId) || !chr.haveItem(consId))
                    {
                        return;
                    }

                    InventoryManipulator.removeFromSlot(c, InventoryTypeUtils.getByType(inventorytype), slot, 1, false, true);
                }
                finally
                {
                    inv.unlockInventory();
                }

                int questid = quest.getId();
                int nextValue = Math.Min(consumables.GetValueOrDefault(consId) + c.getAbstractPlayerInteraction().getQuestProgressInt(questid, infoNumber), consItem.exp * consItem.grade);
                c.getAbstractPlayerInteraction().setQuestProgress(questid, infoNumber, nextValue);

                c.sendPacket(PacketCreator.enableActions());
            }
            finally
            {
                c.releaseClient();
            }
        }
    }
}
