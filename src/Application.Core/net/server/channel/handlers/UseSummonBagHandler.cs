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


using client;
using client.inventory;
using client.inventory.manipulator;
using net.packet;
using server;
using server.life;
using tools;

namespace net.server.channel.handlers;

/**
 * @author AngelSL
 */
public class UseSummonBagHandler : AbstractPacketHandler
{

    public override void handlePacket(InPacket p, Client c)
    {
        //[4A 00][6C 4C F2 02][02 00][63 0B 20 00]
        if (!c.getPlayer().isAlive())
        {
            c.sendPacket(PacketCreator.enableActions());
            return;
        }
        p.readInt();
        short slot = p.readShort();
        int itemId = p.readInt();
        Item toUse = c.getPlayer().getInventory(InventoryType.USE).getItem(slot);
        if (toUse != null && toUse.getQuantity() > 0 && toUse.getItemId() == itemId)
        {
            InventoryManipulator.removeFromSlot(c, InventoryType.USE, slot, 1, false);
            int[][] toSpawn = ItemInformationProvider.getInstance().getSummonMobs(itemId);
            foreach (int[] toSpawnChild in toSpawn)
            {
                if (Randomizer.nextInt(100) < toSpawnChild[1])
                {
                    c.getPlayer().getMap().spawnMonsterOnGroundBelow(LifeFactory.getMonster(toSpawnChild[0]), c.getPlayer().getPosition());
                }
            }
        }
        c.sendPacket(PacketCreator.enableActions());
    }
}
