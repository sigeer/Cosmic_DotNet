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
using net.server.guild;
using tools;

namespace net.server.channel.handlers;

/**
 * @author XoticStory, Ronan
 */
public class AllianceOperationHandler : AbstractPacketHandler
{

    public override void handlePacket(InPacket p, Client c)
    {
        Alliance alliance = null;
        Character chr = c.getPlayer();

        if (chr.getGuild() == null)
        {
            c.sendPacket(PacketCreator.enableActions());
            return;
        }

        if (chr.getGuild().getAllianceId() > 0)
        {
            alliance = chr.getAlliance();
        }

        byte b = p.readByte();
        if (alliance == null)
        {
            if (b != 4)
            {
                c.sendPacket(PacketCreator.enableActions());
                return;
            }
        }
        else
        {
            if (b == 4)
            {
                chr.dropMessage(5, "Your guild is already registered on a guild alliance.");
                c.sendPacket(PacketCreator.enableActions());
                return;
            }

            if (chr.getMGC().getAllianceRank() > 2 || !alliance.getGuilds().Contains(chr.getGuildId()))
            {
                c.sendPacket(PacketCreator.enableActions());
                return;
            }
        }

        // "alliance" is only null at case 0x04
        switch (b)
        {
            case 0x01:
                Server.getInstance().allianceMessage(alliance.getId(), GuildPackets.sendShowInfo(chr.getGuild().getAllianceId(), chr.getId()), -1, -1);
                break;
            case 0x02:
                { // Leave Alliance
                    if (chr.getGuild().getAllianceId() == 0 || chr.getGuildId() < 1 || chr.getGuildRank() != 1)
                    {
                        return;
                    }

                    Alliance.removeGuildFromAlliance(chr.getGuild().getAllianceId(), chr.getGuildId(), chr.getWorld());
                    break;
                }
            case 0x03: // Send Invite
                string guildName = p.readString();

                if (alliance.getGuilds().Count == alliance.getCapacity())
                {
                    chr.dropMessage(5, "Your alliance cannot comport any more guilds at the moment.");
                }
                else
                {
                    Alliance.sendInvitation(c, guildName, alliance.getId());
                }

                break;
            case 0x04:
                { // Accept Invite
                    Guild guild = chr.getGuild();
                    if (guild.getAllianceId() != 0 || chr.getGuildRank() != 1 || chr.getGuildId() < 1)
                    {
                        return;
                    }

                    int allianceid = p.readInt();
                    //slea.readMapleAsciiString();  //recruiter's guild name

                    alliance = Server.getInstance().getAlliance(allianceid);
                    if (alliance == null)
                    {
                        return;
                    }

                    if (!Alliance.answerInvitation(c.getPlayer().getId(), guild.getName(), alliance.getId(), true))
                    {
                        return;
                    }

                    if (alliance.getGuilds().Count == alliance.getCapacity())
                    {
                        chr.dropMessage(5, "Your alliance cannot comport any more guilds at the moment.");
                        return;
                    }

                    int guildid = chr.getGuildId();

                    Server.getInstance().addGuildtoAlliance(alliance.getId(), guildid);
                    Server.getInstance().resetAllianceGuildPlayersRank(guildid);

                    chr.getMGC().setAllianceRank(2);
                    Guild g = Server.getInstance().getGuild(chr.getGuildId());
                    if (g != null)
                    {
                        g.getMGC(chr.getId()).setAllianceRank(2);
                    }

                    chr.saveGuildStatus();

                    Server.getInstance().allianceMessage(alliance.getId(), GuildPackets.addGuildToAlliance(alliance, guildid, c), -1, -1);
                    Server.getInstance().allianceMessage(alliance.getId(), GuildPackets.updateAllianceInfo(alliance, c.getWorld()), -1, -1);
                    Server.getInstance().allianceMessage(alliance.getId(), GuildPackets.allianceNotice(alliance.getId(), alliance.getNotice()), -1, -1);
                    guild.dropMessage("Your guild has joined the [" + alliance.getName() + "] union.");

                    break;
                }
            case 0x06:
                { // Expel Guild
                    int guildid = p.readInt();
                    int allianceid = p.readInt();
                    if (chr.getGuild().getAllianceId() == 0 || chr.getGuild().getAllianceId() != allianceid)
                    {
                        return;
                    }

                    Server.getInstance().allianceMessage(alliance.getId(), GuildPackets.removeGuildFromAlliance(alliance, guildid, c.getWorld()), -1, -1);
                    Server.getInstance().removeGuildFromAlliance(alliance.getId(), guildid);

                    Server.getInstance().allianceMessage(alliance.getId(), GuildPackets.getGuildAlliances(alliance, c.getWorld()), -1, -1);
                    Server.getInstance().allianceMessage(alliance.getId(), GuildPackets.allianceNotice(alliance.getId(), alliance.getNotice()), -1, -1);
                    Server.getInstance().guildMessage(guildid, GuildPackets.disbandAlliance(allianceid));

                    alliance.dropMessage("[" + Server.getInstance().getGuild(guildid).getName() + "] guild has been expelled from the union.");
                    break;
                }
            case 0x07:
                { // Change Alliance Leader
                    if (chr.getGuild().getAllianceId() == 0 || chr.getGuildId() < 1)
                    {
                        return;
                    }
                    int victimid = p.readInt();
                    Character player = Server.getInstance().getWorld(c.getWorld()).getPlayerStorage().getCharacterById(victimid);
                    if (player.getAllianceRank() != 2)
                    {
                        return;
                    }

                    //Server.getInstance().allianceMessage(alliance.getId(), sendChangeLeader(chr.getGuild().getAllianceId(), chr.getId(), slea.readInt()), -1, -1);
                    changeLeaderAllianceRank(alliance, player);
                    break;
                }
            case 0x08:
                string[] ranks = new string[5];
                for (int i = 0; i < 5; i++)
                {
                    ranks[i] = p.readString();
                }
                Server.getInstance().setAllianceRanks(alliance.getId(), ranks);
                Server.getInstance().allianceMessage(alliance.getId(), GuildPackets.changeAllianceRankTitle(alliance.getId(), ranks), -1, -1);
                break;
            case 0x09:
                {
                    int int1 = p.readInt();
                    byte byte1 = p.readByte();

                    //Server.getInstance().allianceMessage(alliance.getId(), sendChangeRank(chr.getGuild().getAllianceId(), chr.getId(), int1, byte1), -1, -1);
                    Character player = Server.getInstance().getWorld(c.getWorld()).getPlayerStorage().getCharacterById(int1);
                    changePlayerAllianceRank(alliance, player, (byte1 > 0));

                    break;
                }
            case 0x0A:
                string notice = p.readString();
                Server.getInstance().setAllianceNotice(alliance.getId(), notice);
                Server.getInstance().allianceMessage(alliance.getId(), GuildPackets.allianceNotice(alliance.getId(), notice), -1, -1);

                alliance.dropMessage(5, "* Alliance Notice : " + notice);
                break;
            default:
                chr.dropMessage("Feature not available");
                break;
        }

        alliance.saveToDB();
    }

    private void changeLeaderAllianceRank(Alliance alliance, Character newLeader)
    {
        GuildCharacter lmgc = alliance.getLeader();
        Character leader = newLeader.getWorldServer().getPlayerStorage().getCharacterById(lmgc.getId());
        leader.getMGC().setAllianceRank(2);
        leader.saveGuildStatus();

        newLeader.getMGC().setAllianceRank(1);
        newLeader.saveGuildStatus();

        Server.getInstance().allianceMessage(alliance.getId(), GuildPackets.getGuildAlliances(alliance, newLeader.getWorld()), -1, -1);
        alliance.dropMessage("'" + newLeader.getName() + "' has been appointed as the new head of this Alliance.");
    }

    private void changePlayerAllianceRank(Alliance alliance, Character chr, bool raise)
    {
        int newRank = chr.getAllianceRank() + (raise ? -1 : 1);
        if (newRank < 3 || newRank > 5)
        {
            return;
        }

        chr.getMGC().setAllianceRank(newRank);
        chr.saveGuildStatus();

        Server.getInstance().allianceMessage(alliance.getId(), GuildPackets.getGuildAlliances(alliance, chr.getWorld()), -1, -1);
        alliance.dropMessage("'" + chr.getName() + "' has been reassigned to '" + alliance.getRankTitle(newRank) + "' in this Alliance.");
    }

}
