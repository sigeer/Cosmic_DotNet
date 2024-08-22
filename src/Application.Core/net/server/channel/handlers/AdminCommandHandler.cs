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
using server.maps;
using server.quest;
using System.Text;
using tools;

namespace net.server.channel.handlers;

public class AdminCommandHandler : AbstractPacketHandler
{

    public override void handlePacket(InPacket p, Client c)
    {
        if (!c.getPlayer().isGM())
        {
            return;
        }
        byte mode = p.readByte();
        string victim;
        Character? target;
        switch (mode)
        {
            case 0x00: // Level1~Level8 & Package1~Package2
                int[][] toSpawn = ItemInformationProvider.getInstance().getSummonMobs(p.readInt());
                foreach (int[] toSpawnChild in toSpawn)
                {
                    if (Randomizer.nextInt(100) < toSpawnChild[1])
                    {
                        c.getPlayer().getMap().spawnMonsterOnGroundBelow(LifeFactory.getMonster(toSpawnChild[0]), c.getPlayer().getPosition());
                    }
                }
                c.sendPacket(PacketCreator.enableActions());
                break;
            case 0x01:
                { // /d (inv)
                    sbyte inventoryType = p.ReadSByte();
                    Inventory inValue = c.getPlayer().getInventory(InventoryTypeUtils.getByType(inventoryType));
                    for (short i = 1; i <= inValue.getSlotLimit(); i++)
                    { //TODO What is the point of this loop?
                        if (inValue.getItem(i) != null)
                        {
                            InventoryManipulator.removeFromSlot(c, InventoryTypeUtils.getByType(inventoryType), i, inValue.getItem(i).getQuantity(), false);
                        }
                        return;
                    }
                    break;
                }
            case 0x02: // Exp
                c.getPlayer().setExp(p.readInt());
                break;
            case 0x03: // /ban <name>
                c.getPlayer().yellowMessage("Please use !ban <IGN> <Reason>");
                break;
            case 0x04: // /block <name> <duration (in days)> <HACK/BOT/AD/HARASS/CURSE/SCAM/MISCONDUCT/SELL/ICASH/TEMP/GM/IPROGRAM/MEGAPHONE>
                victim = p.readString();
                int type = p.readByte(); //reason
                int duration = p.readInt();
                string description = p.readString();
                string reason = c.getPlayer().getName() + " used /ban to ban";
                target = c.getChannelServer().getPlayerStorage().getCharacterByName(victim);
                if (target != null)
                {
                    string readableTargetName = Character.makeMapleReadable(target.getName());
                    string ip = target.getClient().getRemoteAddress();
                    reason += readableTargetName + " (IP: " + ip + ")";
                    if (duration == -1)
                    {
                        target.ban(description + " " + reason);
                    }
                    else
                    {
                        target.block(type, duration, description);
                        target.sendPolice(duration, reason, 6000);
                    }
                    c.sendPacket(PacketCreator.getGMEffect(4, 0));
                }
                else if (Character.ban(victim, reason, false))
                {
                    c.sendPacket(PacketCreator.getGMEffect(4, 0));
                }
                else
                {
                    c.sendPacket(PacketCreator.getGMEffect(6, 1));
                }
                break;
            case 0x10: // /h, information added by vana -- <and tele mode f1> ... hide ofcourse
                c.getPlayer().Hide(p.readByte() == 1);
                break;
            case 0x11: // Entering a map
                switch (p.readByte())
                {
                    case 0:// /u
                        StringBuilder sb = new StringBuilder("USERS ON THIS MAP: ");
                        foreach (Character mc in c.getPlayer().getMap().getCharacters())
                        {
                            sb.Append(mc.getName());
                            sb.Append(" ");
                        }
                        c.getPlayer().message(sb.ToString());
                        break;
                    case 12:// /uclip and entering a map
                        break;
                }
                break;
            case 0x12: // Send
                victim = p.readString();
                int mapId = p.readInt();
                c.getChannelServer().getPlayerStorage().getCharacterByName(victim).changeMap(c.getChannelServer().getMapFactory().getMap(mapId));
                break;
            case 0x15: // Kill
                int mobToKill = p.readInt();
                int amount = p.readInt();
                List<MapObject> monsterx = c.getPlayer().getMap().getMapObjectsInRange(c.getPlayer().getPosition(), double.PositiveInfinity, Arrays.asList(MapObjectType.MONSTER));
                for (int x = 0; x < amount; x++)
                {
                    Monster monster = (Monster)monsterx.get(x);
                    if (monster.getId() == mobToKill)
                    {
                        c.getPlayer().getMap().killMonster(monster, c.getPlayer(), true);
                    }
                }
                break;
            case 0x16: // Questreset
                Quest.getInstance(p.readShort()).reset(c.getPlayer());
                break;
            case 0x17: // Summon
                int mobId = p.readInt();
                int quantity = p.readInt();
                for (int i = 0; i < quantity; i++)
                {
                    c.getPlayer().getMap().spawnMonsterOnGroundBelow(LifeFactory.getMonster(mobId), c.getPlayer().getPosition());
                }
                break;
            case 0x18: // Maple & Mobhp
                int mobHp = p.readInt();
                c.getPlayer().dropMessage("Monsters HP");
                List<MapObject> monsters = c.getPlayer().getMap().getMapObjectsInRange(c.getPlayer().getPosition(), double.PositiveInfinity, Arrays.asList(MapObjectType.MONSTER));
                foreach (MapObject mobs in monsters)
                {
                    Monster monster = (Monster)mobs;
                    if (monster.getId() == mobHp)
                    {
                        c.getPlayer().dropMessage(monster.getName() + ": " + monster.getHp());
                    }
                }
                break;
            case 0x1E: // Warn
                victim = p.readString();
                string message = p.readString();
                target = c.getChannelServer().getPlayerStorage().getCharacterByName(victim);
                if (target != null)
                {
                    target.getClient().sendPacket(PacketCreator.serverNotice(1, message));
                    c.sendPacket(PacketCreator.getGMEffect(0x1E, 1));
                }
                else
                {
                    c.sendPacket(PacketCreator.getGMEffect(0x1E, 0));
                }
                break;
            case 0x24:// /Artifact Ranking
                break;
            case 0x77: //Testing purpose
                if (p.available() == 4)
                {
                    log.Debug("int: {0}", p.readInt());
                }
                else if (p.available() == 2)
                {
                    log.Debug("short: {0}", p.readShort());
                }
                break;
            default:
                log.Information("New GM packet encountered (MODE: {Mode}): {0}", mode, p);
                break;
        }
    }
}
