/*
 This file is part of the OdinMS Maple Story Server
 Copyright (C) 2008 Patrick Huy <patrick.huy@frz.cc>
 Matthias Butz <matze@odinms.de>
 Jan Christian Meyer <vimes@odinms.de>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Affero General Public License as
 published by the Free Software Foundation version 3 as published by
 the Free Software Foundation. You may not use, modify or distribute
 this program under any other version of the GNU Affero General Public
 License.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Affero General Public License for more details.

 You should have received a copy of the GNU Affero General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */


using Application.Core.Game.QuestDomain.RequirementAdapter;
using client.inventory;
using constants.inventory;

namespace server.quest.requirements;




/**
 * @author Tyler (Twdtwd)
 */
public class ItemRequirement : AbstractQuestRequirement
{
    Dictionary<int, int> items = new();

    public ItemRequirement(IRequirementItemAdapter adapter) : base(adapter)
    {
        items = adapter.GetData();
    }

    public override bool check(IPlayer chr, int? npcid)
    {
        ItemInformationProvider ii = ItemInformationProvider.getInstance();
        foreach (int itemId in items.Keys)
        {
            int countNeeded = items.GetValueOrDefault(itemId);
            int count = 0;

            InventoryType iType = ItemConstants.getInventoryType(itemId);

            if (iType.Equals(InventoryType.UNDEFINED))
            {
                return false;
            }
            foreach (Item item in chr.getInventory(iType).listById(itemId))
            {
                count += item.getQuantity();
            }
            //Weird stuff, nexon made some quests only available when wearing gm clothes. This enables us to accept it ><
            if (iType.Equals(InventoryType.EQUIP) && !ItemConstants.isMedal(itemId))
            {
                if (chr.isGM())
                {
                    foreach (Item item in chr.getInventory(InventoryType.EQUIPPED).listById(itemId))
                    {
                        count += item.getQuantity();
                    }
                }
                else
                {
                    if (count < countNeeded)
                    {
                        if (chr.getInventory(InventoryType.EQUIPPED).countById(itemId) + count >= countNeeded)
                        {
                            chr.dropMessage(5, "Unequip the required " + ii.getName(itemId) + " before trying this quest operation.");
                            return false;
                        }
                    }
                }
            }

            if (count < countNeeded || countNeeded <= 0 && count > 0)
            {
                return false;
            }
        }
        return true;
    }

    public int getItemAmountNeeded(int itemid, bool complete)
    {
        var amount = items.get(itemid);
        if (amount != null)
        {
            return amount.Value;
        }
        else
        {
            return complete ? int.MaxValue : int.MinValue;
        }
    }
}
