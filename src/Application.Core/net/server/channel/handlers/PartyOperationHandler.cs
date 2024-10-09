/*
	This file is part of the OdinMS Maple Story NewServer
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


using Application.Core.Managers;
using net.packet;
using net.server.coordinator.world;
using net.server.world;
using tools;

namespace net.server.channel.handlers;



public class PartyOperationHandler : AbstractPacketHandler
{

    public override void HandlePacket(InPacket p, IClient c)
    {
        int operation = p.readByte();
        var player = c.OnlinedCharacter;
        var world = c.getWorldServer();
        var party = player.getParty();
        switch (operation)
        {
            case 1:
                { // create
                    TeamManager.createParty(player, false);
                    break;
                }
            case 2:
                { // leave/disband
                    if (party != null)
                    {
                        var partymembers = player.getPartyMembersOnline();

                        TeamManager.LeaveParty(party, player);
                        player.updatePartySearchAvailability(true);
                        player.partyOperationUpdate(party, partymembers);
                    }
                    break;
                }
            case 3:
                { // join
                    int partyid = p.readInt();

                    InviteResult inviteRes = InviteCoordinator.answerInvite(InviteType.PARTY, player.getId(), partyid, true);
                    InviteResultType res = inviteRes.result;
                    if (res == InviteResultType.ACCEPTED)
                    {
                        TeamManager.joinParty(player, partyid, false);
                    }
                    else
                    {
                        c.sendPacket(PacketCreator.serverNotice(5, "You couldn't join the party due to an expired invitation request."));
                    }
                    break;
                }
            case 4:
                { // invite
                    string name = p.readString();
                    var invited = world.getPlayerStorage().getCharacterByName(name);
                    if (invited != null && invited.IsOnlined)
                    {
                        if (invited.getLevel() < 10 && (!YamlConfig.config.server.USE_PARTY_FOR_STARTERS || player.getLevel() >= 10))
                        { //min requirement is level 10
                            c.sendPacket(PacketCreator.serverNotice(5, "The player you have invited does not meet the requirements."));
                            return;
                        }
                        if (YamlConfig.config.server.USE_PARTY_FOR_STARTERS && invited.getLevel() >= 10 && player.getLevel() < 10)
                        {    //trying to invite high level
                            c.sendPacket(PacketCreator.serverNotice(5, "The player you have invited does not meet the requirements."));
                            return;
                        }

                        if (invited.getParty() == null)
                        {
                            if (party == null)
                            {
                                if (!TeamManager.createParty(player, false))
                                {
                                    return;
                                }

                                party = player.getParty();
                            }
                            if (party.getMembers().Count < 6)
                            {
                                if (InviteCoordinator.createInvite(InviteType.PARTY, player, party.getId(), invited.getId()))
                                {
                                    invited.sendPacket(PacketCreator.partyInvite(player));
                                }
                                else
                                {
                                    c.sendPacket(PacketCreator.partyStatusMessage(22, invited.getName()));
                                }
                            }
                            else
                            {
                                c.sendPacket(PacketCreator.partyStatusMessage(17));
                            }
                        }
                        else
                        {
                            c.sendPacket(PacketCreator.partyStatusMessage(16));
                        }
                    }
                    else
                    {
                        c.sendPacket(PacketCreator.partyStatusMessage(19));
                    }
                    break;
                }
            case 5:
                { // expel
                    int cid = p.readInt();
                    TeamManager.expelFromParty(party, c, cid);
                    break;
                }
            case 6:
                { // change leader
                    int newLeader = p.readInt();
                    var newLeadr = party.getMemberById(newLeader);
                    world.updateParty(party.getId(), PartyOperation.CHANGE_LEADER, newLeadr);
                    break;
                }
        }
    }
}