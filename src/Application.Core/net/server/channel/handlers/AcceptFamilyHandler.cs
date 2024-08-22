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
using Microsoft.EntityFrameworkCore;
using net.packet;
using net.server.coordinator.world;
using tools;

namespace net.server.channel.handlers;





/**
 * @author Jay Estrella
 * @author Ubaware
 */
public class AcceptFamilyHandler : AbstractPacketHandler
{

    public override void handlePacket(InPacket p, Client c)
    {
        if (!YamlConfig.config.server.USE_FAMILY_SYSTEM)
        {
            return;
        }
        Character chr = c.getPlayer();
        int inviterId = p.readInt();
        p.readString();
        bool accept = p.readByte() != 0;
        // string inviterName = slea.readMapleAsciiString();
        var inviter = c.getWorldServer().getPlayerStorage().getCharacterById(inviterId);
        if (inviter != null)
        {
            InviteResult inviteResult = InviteCoordinator.answerInvite(InviteType.FAMILY, c.getPlayer().getId(), c.getPlayer(), accept);
            if (inviteResult.result == InviteResultType.NOT_FOUND)
            {
                return; //was never invited. (or expired on server only somehow?)
            }
            if (accept)
            {
                if (inviter.getFamily() != null)
                {
                    if (chr.getFamily() == null)
                    {
                        FamilyEntry newEntry = new FamilyEntry(inviter.getFamily(), chr.getId(), chr.getName(), chr.getLevel(), chr.getJob());
                        newEntry.setCharacter(chr);
                        if (!newEntry.setSenior(inviter.getFamilyEntry(), true))
                        {
                            inviter.sendPacket(PacketCreator.sendFamilyMessage(1, 0));
                            return;
                        }
                        else
                        {
                            // save
                            inviter.getFamily().addEntry(newEntry);
                            insertNewFamilyRecord(chr.getId(), inviter.getFamily().getID(), inviter.getId(), false);
                        }
                    }
                    else
                    { //absorb target family
                        FamilyEntry targetEntry = chr.getFamilyEntry();
                        Family targetFamily = targetEntry.getFamily();
                        if (targetFamily.getLeader() != targetEntry)
                        {
                            return;
                        }
                        if (inviter.getFamily().getTotalGenerations() + targetFamily.getTotalGenerations() <= YamlConfig.config.server.FAMILY_MAX_GENERATIONS)
                        {
                            targetEntry.join(inviter.getFamilyEntry());
                        }
                        else
                        {
                            inviter.sendPacket(PacketCreator.sendFamilyMessage(76, 0));
                            chr.sendPacket(PacketCreator.sendFamilyMessage(76, 0));
                            return;
                        }
                    }
                }
                else
                { // create new family
                    if (chr.getFamily() != null && inviter.getFamily() != null && chr.getFamily().getTotalGenerations() + inviter.getFamily().getTotalGenerations() >= YamlConfig.config.server.FAMILY_MAX_GENERATIONS)
                    {
                        inviter.sendPacket(PacketCreator.sendFamilyMessage(76, 0));
                        chr.sendPacket(PacketCreator.sendFamilyMessage(76, 0));
                        return;
                    }
                    Family newFamily = new Family(-1, c.getWorld());
                    c.getWorldServer().addFamily(newFamily.getID(), newFamily);
                    FamilyEntry inviterEntry = new FamilyEntry(newFamily, inviter.getId(), inviter.getName(), inviter.getLevel(), inviter.getJob());
                    inviterEntry.setCharacter(inviter);
                    newFamily.setLeader(inviter.getFamilyEntry());
                    newFamily.addEntry(inviterEntry);
                    if (chr.getFamily() == null)
                    { //completely new family
                        FamilyEntry newEntry = new FamilyEntry(newFamily, chr.getId(), chr.getName(), chr.getLevel(), chr.getJob());
                        newEntry.setCharacter(chr);
                        newEntry.setSenior(inviterEntry, true);
                        // save new family
                        insertNewFamilyRecord(inviter.getId(), newFamily.getID(), 0, true);
                        insertNewFamilyRecord(chr.getId(), newFamily.getID(), inviter.getId(), false); // char was already saved from setSenior() above
                        newFamily.setMessage("", true);
                    }
                    else
                    { //new family for inviter, absorb invitee family
                        insertNewFamilyRecord(inviter.getId(), newFamily.getID(), 0, true);
                        newFamily.setMessage("", true);
                        chr.getFamilyEntry().join(inviterEntry);
                    }
                }
                c.getPlayer().getFamily().broadcast(PacketCreator.sendFamilyJoinResponse(true, c.getPlayer().getName()), c.getPlayer().getId());
                c.sendPacket(PacketCreator.getSeniorMessage(inviter.getName()));
                c.sendPacket(PacketCreator.getFamilyInfo(chr.getFamilyEntry()));
                chr.getFamilyEntry().updateSeniorFamilyInfo(true);
            }
            else
            {
                inviter.sendPacket(PacketCreator.sendFamilyJoinResponse(false, c.getPlayer().getName()));
            }
        }
        c.sendPacket(PacketCreator.sendFamilyMessage(0, 0));
    }

    private void insertNewFamilyRecord(int characterID, int familyID, int seniorID, bool updateChar)
    {
        try
        {
            using var dbContext = new DBContext();

            try
            {
                var newModel = new FamilyCharacter(characterID, familyID, seniorID);
                dbContext.FamilyCharacters.Add(newModel);
                dbContext.SaveChanges();
            }
            catch (Exception e)
            {
                log.Error(e, "Could not save new family record for chrId {}", characterID);
            }
            if (updateChar)
            {
                try
                {
                    dbContext.Characters.Where(x => x.Id == characterID).ExecuteUpdate(x => x.SetProperty(y => y.FamilyId, familyID));
                }
                catch (Exception e)
                {
                    log.Error(e, "Could not update 'characters' 'familyid' record for chrId {CharacterId}", characterID);
                }
            }
        }
        catch (Exception e)
        {
            log.Error(e, "Could not get connection to DB while inserting new family record");
        }
    }
}
