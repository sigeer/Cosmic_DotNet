using Application.Core.Managers;
using constants.game;
using constants.id;
using net.packet;
using net.server;
using net.server.coordinator.matchchecker;
using net.server.guild;

namespace Application.Core.net.server.coordinator.matchchecker.listener
{
    public class MatchCheckerGuildCreationListener : AbstractMatchCheckerListener
    {
        private static void broadcastGuildCreationDismiss(HashSet<IPlayer> nonLeaderMatchPlayers)
        {
            foreach (var chr in nonLeaderMatchPlayers)
            {
                if (chr.isLoggedinWorld())
                {
                    chr.sendPacket(GuildPackets.genericGuildMessage(0x26));
                }
            }
        }
        public override void onMatchCreated(IPlayer leader, HashSet<IPlayer> nonLeaderMatchPlayers, string message)
        {
            Packet createGuildPacket = GuildPackets.createGuildMessage(leader.getName(), message);

            foreach (var chr in nonLeaderMatchPlayers)
            {
                if (chr.isLoggedinWorld())
                {
                    chr.sendPacket(createGuildPacket);
                }
            }
        }

        public override void onMatchAccepted(int leaderid, HashSet<IPlayer> matchPlayers, string message)
        {
            var leader = matchPlayers.FirstOrDefault(x => x.getId() == leaderid);

            if (leader == null || !leader.isLoggedinWorld())
            {
                broadcastGuildCreationDismiss(matchPlayers);
                return;
            }
            matchPlayers.Remove(leader);

            if (leader.getGuildId() > 0)
            {
                leader.dropMessage(1, "You cannot create a new Guild while in one.");
                broadcastGuildCreationDismiss(matchPlayers);
                return;
            }
            int partyid = leader.getPartyId();
            if (partyid == -1 || !leader.isPartyLeader())
            {
                leader.dropMessage(1, "You cannot establish the creation of a new Guild without leading a party.");
                broadcastGuildCreationDismiss(matchPlayers);
                return;
            }
            if (leader.getMapId() != MapId.GUILD_HQ)
            {
                leader.dropMessage(1, "You cannot establish the creation of a new Guild outside of the Guild Headquarters.");
                broadcastGuildCreationDismiss(matchPlayers);
                return;
            }
            foreach (var chr in matchPlayers)
            {
                if (leader.getMap().getCharacterById(chr.getId()) == null)
                {
                    leader.dropMessage(1, "You cannot establish the creation of a new Guild if one of the members is not present here.");
                    broadcastGuildCreationDismiss(matchPlayers);
                    return;
                }
            }
            if (leader.getMeso() < YamlConfig.config.server.CREATE_GUILD_COST)
            {
                leader.dropMessage(1, "You do not have " + GameConstants.numberWithCommas(YamlConfig.config.server.CREATE_GUILD_COST) + " mesos to create a Guild.");
                broadcastGuildCreationDismiss(matchPlayers);
                return;
            }

            int gid = Server.getInstance().createGuild(leader.getId(), message);
            if (gid == 0)
            {
                leader.sendPacket(GuildPackets.genericGuildMessage(0x23));
                broadcastGuildCreationDismiss(matchPlayers);
                return;
            }
            leader.gainMeso(-YamlConfig.config.server.CREATE_GUILD_COST, true, false, true);

            leader.setGuildId(gid);
            var guild = Server.getInstance().getGuild(leader.getGuildId(), leader);  // initialize guild structure
            Server.getInstance().changeRank(gid, leader.getId(), 1);

            leader.sendPacket(GuildPackets.showGuildInfo(leader));
            leader.dropMessage(1, "You have successfully created a Guild.");

            foreach (var chr in matchPlayers)
            {
                bool cofounder = chr.getPartyId() == partyid;

                chr.GuildId = gid;
                chr.GuildRank = cofounder ? 2 : 5;
                chr.AllianceRank = 5;

                chr.GuildModel?.addGuildMember(chr);

                if (chr.isLoggedinWorld())
                {
                    chr.sendPacket(GuildPackets.showGuildInfo(chr));

                    if (cofounder)
                    {
                        chr.dropMessage(1, "You have successfully cofounded a Guild.");
                    }
                    else
                    {
                        chr.dropMessage(1, "You have successfully joined the new Guild.");
                    }
                }

                chr.saveGuildStatus(); // update database
            }

            guild.broadcastNameChanged();
            guild.broadcastEmblemChanged();
        }

        public override void onMatchDeclined(int leaderid, HashSet<IPlayer> matchPlayers, string message)
        {
            foreach (var chr in matchPlayers)
            {
                if (chr.getId() == leaderid)
                {
                    TeamManager.LeaveParty(chr.getParty(), chr);
                }

                if (chr.isLoggedinWorld())
                {
                    chr.sendPacket(GuildPackets.genericGuildMessage(0x26));
                }
            }
        }

        public override void onMatchDismissed(int leaderid, HashSet<IPlayer> matchPlayers, string message)
        {

            var leader = matchPlayers.FirstOrDefault(x => x.getId() == leaderid);

            string msg;
            if (leader != null && leader.getParty() == null)
            {
                msg = "The Guild creation has been dismissed since the leader left the founding party.";
            }
            else
            {
                msg = "The Guild creation has been dismissed since a member was already in a party when they answered.";
            }

            foreach (var chr in matchPlayers)
            {
                if (chr.getId() == leaderid)
                {
                    TeamManager.LeaveParty(chr.getParty()!, chr);
                }

                if (chr.isLoggedinWorld())
                {
                    chr.message(msg);
                    chr.sendPacket(GuildPackets.genericGuildMessage(0x26));
                }
            }
        }
    }
}
