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
using constants.game;
using constants.id;
using net.packet;
using net.server.guild;
using net.server.world;
using tools;
using static net.server.coordinator.matchchecker.MatchCheckerListenerFactory;

namespace net.server.channel.handlers;

public class GuildOperationHandler : AbstractPacketHandler
{
    private bool isGuildNameAcceptable(string name)
    {
        if (name.Length < 3 || name.Length > 12)
        {
            return false;
        }
        for (int i = 0; i < name.Length; i++)
        {
            if (!char.IsLower(name.ElementAt(i)) && !char.IsUpper(name.ElementAt(i)))
            {
                return false;
            }
        }
        return true;
    }

    public override void handlePacket(InPacket p, Client c)
    {
        Character mc = c.getPlayer();
        byte type = p.readByte();
        int allianceId = -1;
        switch (type)
        {
            case 0x00:
                //c.sendPacket(PacketCreator.showGuildInfo(mc));
                break;
            case 0x02:
                if (mc.getGuildId() > 0)
                {
                    mc.dropMessage(1, "You cannot create a new Guild while in one.");
                    return;
                }
                if (mc.getMeso() < YamlConfig.config.server.CREATE_GUILD_COST)
                {
                    mc.dropMessage(1, "You do not have " + GameConstants.numberWithCommas(YamlConfig.config.server.CREATE_GUILD_COST) + " mesos to create a Guild.");
                    return;
                }
                string guildName = p.readString();
                if (!isGuildNameAcceptable(guildName))
                {
                    mc.dropMessage(1, "The Guild name you have chosen is not accepted.");
                    return;
                }

                HashSet<Character> eligibleMembers = new(Guild.getEligiblePlayersForGuild(mc));
                if (eligibleMembers.Count < YamlConfig.config.server.CREATE_GUILD_MIN_PARTNERS)
                {
                    if (mc.getMap().getAllPlayers().Count < YamlConfig.config.server.CREATE_GUILD_MIN_PARTNERS)
                    {
                        // thanks NovaStory for noticing message in need of smoother info
                        mc.dropMessage(1, "Your Guild doesn't have enough cofounders present here and therefore cannot be created at this time.");
                    }
                    else
                    {
                        // players may be unaware of not belonging on a party in order to become eligible, thanks Hair (Legalize) for pointing this out
                        mc.dropMessage(1, "Please make sure everyone you are trying to invite is neither on a guild nor on a party.");
                    }

                    return;
                }

                if (!Party.createParty(mc, true))
                {
                    mc.dropMessage(1, "You cannot create a new Guild while in a party.");
                    return;
                }

                HashSet<int> eligibleCids = new();
                foreach (Character chr in eligibleMembers)
                {
                    eligibleCids.Add(chr.getId());
                }

                c.getWorldServer().getMatchCheckerCoordinator().createMatchConfirmation(MatchCheckerType.GUILD_CREATION, c.getWorld(), mc.getId(), eligibleCids, guildName);
                break;
            case 0x05:
                if (mc.getGuildId() <= 0 || mc.getGuildRank() > 2)
                {
                    return;
                }

                string targetName = p.readString();
                var mgr = Guild.sendInvitation(c, targetName);
                if (mgr != null)
                {
                    c.sendPacket(mgr.Value.getPacket(targetName));
                }
                else
                {
                } // already sent invitation, do nothing

                break;
            case 0x06:
                if (mc.getGuildId() > 0)
                {
                    log.Warning("[Hack] Chr {CharacterName} attempted to join a guild when s/he is already in one.", mc.getName());
                    return;
                }
                int gid = p.readInt();
                int cid = p.readInt();
                if (cid != mc.getId())
                {
                    log.Warning("[Hack] Chr {CharacterName} attempted to join a guild with a different chrId", mc.getName());
                    return;
                }

                if (!Guild.answerInvitation(cid, mc.getName(), gid, true))
                {
                    return;
                }

                mc.getMGC().setGuildId(gid); // joins the guild
                mc.getMGC().setGuildRank(5); // start at lowest rank
                mc.getMGC().setAllianceRank(5);

                int s = Server.getInstance().addGuildMember(mc.getMGC(), mc);
                if (s == 0)
                {
                    mc.dropMessage(1, "The guild you are trying to join is already full.");
                    mc.getMGC().setGuildId(0);
                    return;
                }

                c.sendPacket(GuildPackets.showGuildInfo(mc));

                allianceId = mc.getGuild().getAllianceId();
                if (allianceId > 0)
                {
                    Server.getInstance().getAlliance(allianceId).updateAlliancePackets(mc);
                }

                mc.saveGuildStatus(); // update database
                mc.getMap().broadcastPacket(mc, GuildPackets.guildNameChanged(mc.getId(), mc.getGuild().getName())); // thanks Vcoc for pointing out an issue with updating guild tooltip to players in the map
                mc.getMap().broadcastPacket(mc, GuildPackets.guildMarkChanged(mc.getId(), mc.getGuild()));
                break;
            case 0x07:
                cid = p.readInt();
                string name = p.readString();
                if (cid != mc.getId() || !name.Equals(mc.getName()) || mc.getGuildId() <= 0)
                {
                    log.Warning("[Hack] Chr {CharacterName} tried to quit guild under the name {} and current guild id of {}", mc.getName(), name, mc.getGuildId());
                    return;
                }

                allianceId = mc.getGuild().getAllianceId();

                c.sendPacket(GuildPackets.updateGP(mc.getGuildId(), 0));
                Server.getInstance().leaveGuild(mc.getMGC());

                c.sendPacket(GuildPackets.showGuildInfo(null));
                if (allianceId > 0)
                {
                    Server.getInstance().getAlliance(allianceId).updateAlliancePackets(mc);
                }

                mc.getMGC().setGuildId(0);
                mc.getMGC().setGuildRank(5);
                mc.saveGuildStatus();
                mc.getMap().broadcastPacket(mc, GuildPackets.guildNameChanged(mc.getId(), ""));
                break;
            case 0x08:
                allianceId = mc.getGuild().getAllianceId();

                cid = p.readInt();
                name = p.readString();
                if (mc.getGuildRank() > 2 || mc.getGuildId() <= 0)
                {
                    log.Warning("[Hack] Chr {CharacterName} is trying to expel without rank 1 or 2", mc.getName());
                    return;
                }

                Server.getInstance().expelMember(mc.getMGC(), name, cid);
                if (allianceId > 0)
                {
                    Server.getInstance().getAlliance(allianceId).updateAlliancePackets(mc);
                }
                break;
            case 0x0d:
                if (mc.getGuildId() <= 0 || mc.getGuildRank() != 1)
                {
                    log.Warning("[Hack] Chr {CharacterName} tried to change guild rank titles when s/he does not have permission", mc.getName());
                    return;
                }
                string[] ranks = new string[5];
                for (int i = 0; i < 5; i++)
                {
                    ranks[i] = p.readString();
                }

                Server.getInstance().changeRankTitle(mc.getGuildId(), ranks);
                break;
            case 0x0e:
                cid = p.readInt();
                byte newRank = p.readByte();
                if (mc.getGuildRank() > 2 || (newRank <= 2 && mc.getGuildRank() != 1) || mc.getGuildId() <= 0)
                {
                    log.Warning("[Hack] Chr {CharacterName} is trying to change rank outside of his/her permissions.", mc.getName());
                    return;
                }
                if (newRank <= 1 || newRank > 5)
                {
                    return;
                }
                Server.getInstance().changeRank(mc.getGuildId(), cid, newRank);
                break;
            case 0x0f:
                if (mc.getGuildId() <= 0 || mc.getGuildRank() != 1 || mc.getMapId() != MapId.GUILD_HQ)
                {
                    log.Warning("[Hack] Chr {CharacterName} tried to change guild emblem without being the guild leader", mc.getName());
                    return;
                }
                if (mc.getMeso() < YamlConfig.config.server.CHANGE_EMBLEM_COST)
                {
                    c.sendPacket(PacketCreator.serverNotice(1, "You do not have " + GameConstants.numberWithCommas(YamlConfig.config.server.CHANGE_EMBLEM_COST) + " mesos to change the Guild emblem."));
                    return;
                }
                short bg = p.readShort();
                byte bgcolor = p.readByte();
                short logo = p.readShort();
                byte logocolor = p.readByte();
                Server.getInstance().setGuildEmblem(mc.getGuildId(), bg, bgcolor, logo, logocolor);

                if (mc.getGuild() != null && mc.getGuild().getAllianceId() > 0)
                {
                    var alliance = mc.getAlliance();
                    Server.getInstance().allianceMessage(alliance.getId(), GuildPackets.getGuildAlliances(alliance, c.getWorld()), -1, -1);
                }

                mc.gainMeso(-YamlConfig.config.server.CHANGE_EMBLEM_COST, true, false, true);
                mc.getGuild().broadcastNameChanged();
                mc.getGuild().broadcastEmblemChanged();
                break;
            case 0x10:
                if (mc.getGuildId() <= 0 || mc.getGuildRank() > 2)
                {
                    if (mc.getGuildId() <= 0)
                    {
                        log.Warning("[Hack] Chr {CharacterName} tried to change guild notice while not in a guild", mc.getName());
                    }
                    return;
                }
                string notice = p.readString();
                if (notice.Length > 100)
                {
                    return;
                }
                Server.getInstance().setGuildNotice(mc.getGuildId(), notice);
                break;
            case 0x1E:
                p.readInt();
                World wserv = c.getWorldServer();

                if (mc.getParty() != null)
                {
                    wserv.getMatchCheckerCoordinator().dismissMatchConfirmation(mc.getId());
                    return;
                }

                int leaderid = wserv.getMatchCheckerCoordinator().getMatchConfirmationLeaderid(mc.getId());
                if (leaderid != -1)
                {
                    bool result = p.readByte() != 0;
                    if (result && wserv.getMatchCheckerCoordinator().isMatchConfirmationActive(mc.getId()))
                    {
                        var leader = wserv.getPlayerStorage().getCharacterById(leaderid);
                        if (leader != null)
                        {
                            int partyid = leader.getPartyId();
                            if (partyid != -1)
                            {
                                Party.joinParty(mc, partyid, true);    // GMS gimmick "party to form guild" recalled thanks to Vcoc
                            }
                        }
                    }

                    wserv.getMatchCheckerCoordinator().answerMatchConfirmation(mc.getId(), result);
                }

                break;
            default:
                log.Warning("Unhandled GUILD_OPERATION packet: {}", p);
                break;
        }
    }
}
