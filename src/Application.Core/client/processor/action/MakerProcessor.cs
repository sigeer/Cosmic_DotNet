/*
    This file is part of the HeavenMS MapleStory Server
    Copyleft (L) 2016 - 2019 RonanLana

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
using constants.game;
using constants.id;
using constants.inventory;
using net.packet;
using server;
using tools;
using static server.MakerItemFactory;


/**
 * @author Ronan
 */
public class MakerProcessor
{
    private static ILogger log = LogFactory.GetLogger(LogType.Maker);
    private static ItemInformationProvider ii = ItemInformationProvider.getInstance();

    public static void makerAction(InPacket p, IClient c)
    {
        if (c.tryacquireClient())
        {
            try
            {
                int type = p.readInt();
                int toCreate = p.readInt();
                int toDisassemble = -1, pos = -1;
                bool makerSucceeded = true;

                MakerItemCreateEntry recipe;
                Dictionary<int, short> reagentids = new();
                int stimulantid = -1;

                if (type == 3)
                {    // building monster crystal
                    int fromLeftover = toCreate;
                    toCreate = ii.getMakerCrystalFromLeftover(toCreate);
                    if (toCreate == -1)
                    {
                        c.sendPacket(PacketCreator.serverNotice(1, ii.getName(fromLeftover) + " is unavailable for Monster Crystal conversion."));
                        c.sendPacket(PacketCreator.makerEnableActions());
                        return;
                    }

                    recipe = MakerItemFactory.generateLeftoverCrystalEntry(fromLeftover, toCreate);
                }
                else if (type == 4)
                {  // disassembling
                    p.readInt(); // 1... probably inventory type
                    pos = p.readInt();

                    var it = c.OnlinedCharacter.getInventory(InventoryType.EQUIP).getItem((short)pos);
                    if (it != null && it.getItemId() == toCreate)
                    {
                        toDisassemble = toCreate;

                        var pair = generateDisassemblyInfo(toDisassemble);
                        if (pair != null)
                        {
                            recipe = MakerItemFactory.generateDisassemblyCrystalEntry(toDisassemble, pair.Value.Key, pair.Value.Value);
                        }
                        else
                        {
                            c.sendPacket(PacketCreator.serverNotice(1, ii.getName(toCreate) + " is unavailable for Monster Crystal disassembly."));
                            c.sendPacket(PacketCreator.makerEnableActions());
                            return;
                        }
                    }
                    else
                    {
                        c.sendPacket(PacketCreator.serverNotice(1, "An unknown error occurred when trying to apply that item for disassembly."));
                        c.sendPacket(PacketCreator.makerEnableActions());
                        return;
                    }
                }
                else
                {
                    if (ItemConstants.isEquipment(toCreate))
                    {   // only equips uses stimulant and reagents
                        if (p.readByte() != 0)
                        {  // stimulant
                            stimulantid = ii.getMakerStimulant(toCreate);
                            if (!c.getAbstractPlayerInteraction().haveItem(stimulantid))
                            {
                                stimulantid = -1;
                            }
                        }

                        int reagents = Math.Min(p.readInt(), getMakerReagentSlots(toCreate));
                        for (int i = 0; i < reagents; i++)
                        {  // crystals
                            int reagentid = p.readInt();
                            if (ItemConstants.isMakerReagent(reagentid))
                            {
                                var rs = reagentids.GetValueOrDefault(reagentid);
                                reagentids.AddOrUpdate(reagentid, (short)(rs + 1));
                            }
                        }

                        List<KeyValuePair<int, short>> toUpdate = new();
                        foreach (var r in reagentids)
                        {
                            int qty = c.getAbstractPlayerInteraction().getItemQuantity(r.Key);

                            if (qty < r.Value)
                            {
                                toUpdate.Add(new(r.Key, (short)qty));
                            }
                        }

                        // remove those not present on player inventory
                        if (toUpdate.Count > 0)
                        {
                            foreach (var rp in toUpdate)
                            {
                                if (rp.Value > 0)
                                {
                                    reagentids.AddOrUpdate(rp.Key, rp.Value);
                                }
                                else
                                {
                                    reagentids.Remove(rp.Key);
                                }
                            }
                        }

                        if (reagentids.Count > 0)
                        {
                            if (!removeOddMakerReagents(toCreate, reagentids))
                            {
                                c.sendPacket(PacketCreator.serverNotice(1, "You can only use WATK and MATK Strengthening Gems on weapon items."));
                                c.sendPacket(PacketCreator.makerEnableActions());
                                return;
                            }
                        }
                    }

                    recipe = MakerItemFactory.getItemCreateEntry(toCreate, stimulantid, reagentids);
                }

                short createStatus = getCreateStatus(c, recipe);

                switch (createStatus)
                {
                    case -1:// non-available for Maker itemid has been tried to forge
                        log.Warning("Chr {CharacterName} tried to craft itemid {ItemId} using the Maker skill.", c.OnlinedCharacter.getName(), toCreate);
                        c.sendPacket(PacketCreator.serverNotice(1, "The requested item could not be crafted on this operation."));
                        c.sendPacket(PacketCreator.makerEnableActions());
                        break;

                    case 1: // no items
                        c.sendPacket(PacketCreator.serverNotice(1, "You don't have all required items in your inventory to make " + ii.getName(toCreate) + "."));
                        c.sendPacket(PacketCreator.makerEnableActions());
                        break;

                    case 2: // no meso
                        c.sendPacket(PacketCreator.serverNotice(1, "You don't have enough mesos (" + GameConstants.numberWithCommas(recipe.getCost()) + ") to complete this operation."));
                        c.sendPacket(PacketCreator.makerEnableActions());
                        break;

                    case 3: // no req level
                        c.sendPacket(PacketCreator.serverNotice(1, "You don't have enough level to complete this operation."));
                        c.sendPacket(PacketCreator.makerEnableActions());
                        break;

                    case 4: // no req skill level
                        c.sendPacket(PacketCreator.serverNotice(1, "You don't have enough Maker level to complete this operation."));
                        c.sendPacket(PacketCreator.makerEnableActions());
                        break;

                    case 5: // inventory full
                        c.sendPacket(PacketCreator.serverNotice(1, "Your inventory is full."));
                        c.sendPacket(PacketCreator.makerEnableActions());
                        break;

                    default:
                        if (toDisassemble != -1)
                        {
                            InventoryManipulator.removeFromSlot(c, InventoryType.EQUIP, (short)pos, 1, false);
                        }
                        else
                        {
                            foreach (var pair in recipe.getReqItems())
                            {
                                c.getAbstractPlayerInteraction().gainItem(pair.ItemId, (short)-pair.Quantity, false);
                            }
                        }

                        int cost = recipe.getCost();
                        if (stimulantid == -1 && reagentids.Count == 0)
                        {
                            if (cost > 0)
                            {
                                c.OnlinedCharacter.gainMeso(-cost, false);
                            }

                            foreach (var pair in recipe.getGainItems())
                            {
                                c.OnlinedCharacter.setCS(true);
                                c.getAbstractPlayerInteraction().gainItem(pair.ItemId, (short)pair.Quantity, false);
                                c.OnlinedCharacter.setCS(false);
                            }
                        }
                        else
                        {
                            toCreate = recipe.getGainItems().get(0).ItemId;

                            if (stimulantid != -1)
                            {
                                c.getAbstractPlayerInteraction().gainItem(stimulantid, -1, false);
                            }
                            if (reagentids.Count > 0)
                            {
                                foreach (var r in reagentids)
                                {
                                    c.getAbstractPlayerInteraction().gainItem(r.Key, (short)(-1 * r.Value), false);
                                }
                            }

                            if (cost > 0)
                            {
                                c.OnlinedCharacter.gainMeso(-cost, false);
                            }
                            makerSucceeded = addBoostedMakerItem(c, toCreate, stimulantid, reagentids);
                        }

                        // thanks inhyuk for noticing missing MAKER_RESULT packets
                        if (type == 3)
                        {
                            c.sendPacket(PacketCreator.makerResultCrystal(recipe.getGainItems().get(0).ItemId, recipe.getReqItems().get(0).ItemId));
                        }
                        else if (type == 4)
                        {
                            c.sendPacket(PacketCreator.makerResultDesynth(recipe.getReqItems().get(0).ItemId, recipe.getCost(), recipe.getGainItems()));
                        }
                        else
                        {
                            c.sendPacket(PacketCreator.makerResult(makerSucceeded, recipe.getGainItems().get(0).ItemId, recipe.getGainItems().get(0).Quantity, recipe.getCost(), recipe.getReqItems(), stimulantid, new(reagentids.Keys)));
                        }

                        c.sendPacket(PacketCreator.showMakerEffect(makerSucceeded));
                        c.OnlinedCharacter.getMap().broadcastMessage(c.OnlinedCharacter, PacketCreator.showForeignMakerEffect(c.OnlinedCharacter.getId(), makerSucceeded), false);

                        if (toCreate == 4260003 && type == 3 && c.OnlinedCharacter.getQuestStatus(6033) == 1)
                        {
                            c.getAbstractPlayerInteraction().setQuestProgress(6033, 1);
                        }
                        break;
                }
            }
            finally
            {
                c.releaseClient();
            }
        }
    }

    // checks and prevents hackers from PE'ing Maker operations with invalid operations
    private static bool removeOddMakerReagents(int toCreate, Dictionary<int, short> reagentids)
    {
        Dictionary<int, int> reagentType = new();
        List<int> toRemove = new();

        bool isWeapon = ItemConstants.isWeapon(toCreate) || YamlConfig.config.server.USE_MAKER_PERMISSIVE_ATKUP;  // thanks Vcoc for finding a case where a weapon wouldn't be counted as such due to a bounding on isWeapon

        foreach (var r in reagentids)
        {
            int curRid = r.Key;
            int type = r.Key / 100;

            if (type < 42502 && !isWeapon)
            {     // only weapons should gain w.att/m.att from these.
                return false;   //toRemove.Add(curRid);
            }
            else
            {
                if (reagentType.TryGetValue(type, out var tableRid))
                {
                    if (tableRid < curRid)
                    {
                        toRemove.Add(tableRid);
                        reagentType.AddOrUpdate(type, curRid);
                    }
                    else
                    {
                        toRemove.Add(curRid);
                    }
                }
                else
                {
                    reagentType.AddOrUpdate(type, curRid);
                }
            }
        }

        // removing less effective gems of repeated type
        foreach (int i in toRemove)
        {
            reagentids.Remove(i);
        }

        // the Maker skill will use only one of each gem
        foreach (int i in reagentids.Keys)
        {
            reagentids.AddOrUpdate(i, (short)1);
        }

        return true;
    }

    private static int getMakerReagentSlots(int itemId)
    {
        try
        {
            int eqpLevel = ii.getEquipLevelReq(itemId);

            if (eqpLevel < 78)
            {
                return 1;
            }
            else if (eqpLevel >= 78 && eqpLevel < 108)
            {
                return 2;
            }
            else
            {
                return 3;
            }
        }
        catch
        {
            return 0;
        }
    }

    private static KeyValuePair<int, List<ItemQuantity>>? generateDisassemblyInfo(int itemId)
    {
        int recvFee = ii.getMakerDisassembledFee(itemId);
        if (recvFee > -1)
        {
            var gains = ii.getMakerDisassembledItems(itemId);
            if (gains.Count > 0)
            {
                return new(recvFee, gains);
            }
        }

        return null;
    }

    public static int getMakerSkillLevel(IPlayer chr)
    {
        return chr.getSkillLevel((chr.getJob().getId() / 1000) * 10000000 + 1007);
    }

    private static short getCreateStatus(IClient c, MakerItemCreateEntry recipe)
    {
        if (recipe.isInvalid())
        {
            return -1;
        }

        if (!hasItems(c, recipe))
        {
            return 1;
        }

        if (c.OnlinedCharacter.getMeso() < recipe.getCost())
        {
            return 2;
        }

        if (c.OnlinedCharacter.getLevel() < recipe.getReqLevel())
        {
            return 3;
        }

        if (getMakerSkillLevel(c.OnlinedCharacter) < recipe.getReqSkillLevel())
        {
            return 4;
        }

        List<int> addItemids = new();
        List<int> addQuantity = new();
        List<int> rmvItemids = new();
        List<int> rmvQuantity = new();

        foreach (var p in recipe.getReqItems())
        {
            rmvItemids.Add(p.ItemId);
            rmvQuantity.Add(p.Quantity);
        }

        foreach (var p in recipe.getGainItems())
        {
            addItemids.Add(p.ItemId);
            addQuantity.Add(p.Quantity);
        }

        if (!c.getAbstractPlayerInteraction().canHoldAllAfterRemoving(addItemids, addQuantity, rmvItemids, rmvQuantity))
        {
            return 5;
        }

        return 0;
    }

    private static bool hasItems(IClient c, MakerItemCreateEntry recipe)
    {
        foreach (var p in recipe.getReqItems())
        {
            int itemId = p.ItemId;
            if (c.OnlinedCharacter.countItem(itemId) < p.Quantity)
            {
                return false;
            }
        }
        return true;
    }

    private static bool addBoostedMakerItem(IClient c, int itemid, int stimulantid, Dictionary<int, short> reagentids)
    {
        if (stimulantid != -1 && !ItemInformationProvider.rollSuccessChance(90.0))
        {
            return false;
        }

        var item = ii.getEquipById(itemid);
        if (item == null)
        {
            return false;
        }

        Equip eqp = (Equip)item;
        if (ItemConstants.isAccessory(item.getItemId()) && eqp.getUpgradeSlots() <= 0)
        {
            eqp.setUpgradeSlots(3);
        }

        if (YamlConfig.config.server.USE_ENHANCED_CRAFTING)
        {
            if (!(c.OnlinedCharacter.isGM() && YamlConfig.config.server.USE_PERFECT_GM_SCROLL))
            {
                eqp.setUpgradeSlots((byte)(eqp.getUpgradeSlots() + 1));
            }
            item = ItemInformationProvider.getInstance().scrollEquipWithId(eqp, ItemId.CHAOS_SCROll_60, true, ItemId.CHAOS_SCROll_60, c.OnlinedCharacter.isGM());
        }

        if (reagentids.Count > 0)
        {
            Dictionary<string, int> stats = new();
            List<short> randOption = new();
            List<short> randStat = new();

            foreach (var r in reagentids)
            {
                var reagentBuff = ii.getMakerReagentStatUpgrade(r.Key);

                if (reagentBuff != null)
                {
                    string s = reagentBuff.Value.Key;

                    if (s.Substring(0, 4).Contains("rand"))
                    {
                        if (s.Substring(4).Equals("Stat"))
                        {
                            randStat.Add((short)(reagentBuff.Value.Value * r.Value));
                        }
                        else
                        {
                            randOption.Add((short)(reagentBuff.Value.Value * r.Value));
                        }
                    }
                    else
                    {
                        string stat = s.Substring(3);

                        if (!stat.Equals("ReqLevel"))
                        {    // improve req level... really?
                            switch (stat)
                            {
                                case "MaxHP":
                                    stat = "MHP";
                                    break;

                                case "MaxMP":
                                    stat = "MMP";
                                    break;
                            }

                            var d = stats.GetValueOrDefault(stat);
                            stats.AddOrUpdate(stat, d + (reagentBuff.Value.Value * r.Value));
                        }
                    }
                }
            }

            ItemInformationProvider.improveEquipStats(eqp, stats);

            foreach (short sh in randStat)
            {
                ii.scrollOptionEquipWithChaos(eqp, sh, false);
            }

            foreach (short sh in randOption)
            {
                ii.scrollOptionEquipWithChaos(eqp, sh, true);
            }
        }

        if (stimulantid != -1)
        {
            eqp = ii.randomizeUpgradeStats(eqp);
        }

        InventoryManipulator.addFromDrop(c, item, false, -1);
        return true;
    }
}
