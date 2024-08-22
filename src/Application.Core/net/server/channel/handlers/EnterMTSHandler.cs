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
using net.packet;
using server;
using server.maps;
using tools;

namespace net.server.channel.handlers;

public class EnterMTSHandler : AbstractPacketHandler
{

    public override void handlePacket(InPacket p, Client c)
    {
        Character chr = c.getPlayer();

        if (!YamlConfig.config.server.USE_MTS)
        {
            c.sendPacket(PacketCreator.enableActions());
            return;
        }

        if (chr.getEventInstance() != null)
        {
            c.sendPacket(PacketCreator.serverNotice(5, "Entering Cash Shop or MTS are disabled when registered on an event."));
            c.sendPacket(PacketCreator.enableActions());
            return;
        }

        if (MiniDungeonInfo.isDungeonMap(chr.getMapId()))
        {
            c.sendPacket(PacketCreator.serverNotice(5, "Changing channels or entering Cash Shop or MTS are disabled when inside a Mini-Dungeon."));
            c.sendPacket(PacketCreator.enableActions());
            return;
        }

        if (FieldLimit.CANNOTMIGRATE.check(chr.getMap().getFieldLimit()))
        {
            chr.dropMessage(1, "You can't do it here in this map.");
            c.sendPacket(PacketCreator.enableActions());
            return;
        }

        if (!chr.isAlive())
        {
            c.sendPacket(PacketCreator.enableActions());
            return;
        }
        if (chr.getLevel() < 10)
        {
            c.sendPacket(PacketCreator.blockedMessage2(5));
            c.sendPacket(PacketCreator.enableActions());
            return;
        }

        chr.closePlayerInteractions();
        chr.closePartySearchInteractions();

        chr.unregisterChairBuff();
        Server.getInstance().getPlayerBuffStorage().addBuffsToStorage(chr.getId(), chr.getAllBuffs());
        Server.getInstance().getPlayerBuffStorage().addDiseasesToStorage(chr.getId(), chr.getAllDiseases());
        chr.setAwayFromChannelWorld();
        chr.notifyMapTransferToPartner(-1);
        chr.removeIncomingInvites();
        chr.cancelAllBuffs(true);
        chr.cancelAllDebuffs();
        chr.cancelBuffExpireTask();
        chr.cancelDiseaseExpireTask();
        chr.cancelSkillCooldownTask();
        chr.cancelExpirationTask();

        chr.forfeitExpirableQuests();
        chr.cancelQuestExpirationTask();

        chr.saveCharToDB();

        c.getChannelServer().removePlayer(chr);
        chr.getMap().removePlayer(c.getPlayer());
        try
        {
            c.sendPacket(PacketCreator.openCashShop(c, true));
        }
        catch (Exception ex)
        {
            log.Error(ex.ToString());
        }
        chr.getCashShop().open(true);// xD
        c.enableCSActions();
        c.sendPacket(PacketCreator.MTSWantedListingOver(0, 0));
        c.sendPacket(PacketCreator.showMTSCash(c.getPlayer()));
        List<MTSItemInfo> items = new();
        int pages = 0;
        try
        {
            using var dbContext = new DBContext();
            items = dbContext.MtsItems.Where(x => x.Tab == 1 && x.Transfer == 0).OrderByDescending(x => x.Id).Skip(16).Take(16).ToList().Select(MTSItemInfo.Map).ToList();
            pages = (int)Math.Ceiling((double)dbContext.MtsItems.Count() / 16);

        }
        catch (Exception e)
        {
            log.Error(e.ToString());
        }
        c.sendPacket(PacketCreator.sendMTS(items, 1, 0, 0, pages));
        c.sendPacket(PacketCreator.transferInventory(getTransfer(chr.getId())));
        c.sendPacket(PacketCreator.notYetSoldInv(getNotYetSold(chr.getId())));
    }

    private List<MTSItemInfo> getNotYetSold(int cid)
    {
        List<MTSItemInfo> items = new();
        try
        {
            using var dbContext = new DBContext();
            return dbContext.MtsItems.Where(x => x.Seller == cid && x.Transfer == 0).OrderByDescending(x => x.Id).ToList().Select(MTSItemInfo.Map).ToList();
        }
        catch (Exception e)
        {
            log.Error(e.ToString());
        }
        return items;
    }

    private List<MTSItemInfo> getTransfer(int cid)
    {
        List<MTSItemInfo> items = new();
        try
        {
            using var dbContext = new DBContext();
            return dbContext.MtsItems.Where(x => x.Seller == cid && x.Transfer == 1).OrderByDescending(x => x.Id).ToList().Select(MTSItemInfo.Map).ToList();
        }
        catch (Exception e)
        {
            log.Error(e.ToString());
        }
        return items;
    }
}
