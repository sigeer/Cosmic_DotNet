using Application.Core.Game.Relation;
using net.server.coordinator.matchchecker;
using net.server.world;
using tools;

namespace Application.Core.Managers
{
    public class TeamManager
    {
        public static bool createParty(IPlayer player, bool silentCheck)
        {
            var party = player.getParty();
            if (party == null)
            {
                if (player.Level < 10 && !YamlConfig.config.server.USE_PARTY_FOR_STARTERS)
                {
                    player.sendPacket(PacketCreator.partyStatusMessage(10));
                    return false;
                }
                else if (player.getAriantColiseum() != null)
                {
                    player.dropMessage(5, "You cannot request a party creation while participating the Ariant Battle Arena.");
                    return false;
                }

                party = player.getWorldServer().createParty(player);
                player.setParty(party);
                // player.setMPC(partyplayer);
                player.getMap().addPartyMember(player, party.getId());
                player.silentPartyUpdate();

                player.updatePartySearchAvailability(false);
                player.partyOperationUpdate(party, null);

                player.sendPacket(PacketCreator.partyCreated(party, player.Id));

                return true;
            }
            else
            {
                if (!silentCheck)
                {
                    player.sendPacket(PacketCreator.partyStatusMessage(16));
                }

                return false;
            }
        }

        public static void LeaveParty(ITeam? party, IPlayer? player)
        {
            if (party != null && player != null)
            {
                var world = player.getWorldServer();

                if (player.Id == party.getLeaderId())
                {
                    world.removeMapPartyMembers(party.getId());

                    var mcpq = player.getMonsterCarnival();
                    if (mcpq != null)
                    {
                        mcpq.leftParty(player.getId());
                    }

                    world.updateParty(party.getId(), PartyOperation.DISBAND, player);

                    var eim = player.getEventInstance();
                    if (eim != null)
                    {
                        eim.disbandParty();
                    }
                }
                else
                {
                    var map = player.getMap();
                    if (map != null)
                    {
                        map.removePartyMember(player, party.getId());
                    }

                    var mcpq = player.getMonsterCarnival();
                    if (mcpq != null)
                    {
                        mcpq.leftParty(player.getId());
                    }

                    world.updateParty(party.getId(), PartyOperation.LEAVE, player);

                    var eim = player.getEventInstance();
                    if (eim != null)
                    {
                        eim.leftParty(player);
                    }
                }

                player.setParty(null);

                MatchCheckerCoordinator mmce = world.getMatchCheckerCoordinator();
                if (mmce.getMatchConfirmationLeaderid(player.getId()) == player.getId() && mmce.getMatchConfirmationType(player.getId()) == MatchCheckerType.GUILD_CREATION)
                {
                    mmce.dismissMatchConfirmation(player.getId());
                }
            }
        }

        public static bool joinParty(IPlayer player, int partyid, bool silentCheck)
        {
            var party = player.getParty();
            var world = player.getWorldServer();

            if (party == null)
            {
                party = world.getParty(partyid);
                if (party != null)
                {
                    if (party.getMembers().Count < 6)
                    {
                        player.getMap().addPartyMember(player, party.getId());

                        world.updateParty(party.getId(), PartyOperation.JOIN, player);
                        player.receivePartyMemberHP();
                        player.updatePartyMemberHP();

                        player.resetPartySearchInvite(party.getLeaderId());
                        player.updatePartySearchAvailability(false);
                        player.partyOperationUpdate(party, null);
                        return true;
                    }
                    else
                    {
                        if (!silentCheck)
                        {
                            player.sendPacket(PacketCreator.partyStatusMessage(17));
                        }
                    }
                }
                else
                {
                    player.sendPacket(PacketCreator.serverNotice(5, "You couldn't join the party since it had already been disbanded."));
                }
            }
            else
            {
                if (!silentCheck)
                {
                    player.sendPacket(PacketCreator.serverNotice(5, "You can't join the party as you are already in one."));
                }
            }

            return false;
        }

        public static void expelFromParty(ITeam party, IClient c, int expelCid)
        {
            var world = c.getWorldServer();
            var player = c.getPlayer();

            if (party != null && player != null)
            {
                if (player.Equals(party.getLeader()))
                {
                    var emc = party.getMemberById(expelCid);

                    if (emc != null)
                    {
                        var partyMembers = emc.getPartyMembersOnline();

                        var map = emc.getMap();
                        if (map != null)
                        {
                            map.removePartyMember(emc, party.getId());
                        }

                        var mcpq = player.getMonsterCarnival();
                        if (mcpq != null)
                        {
                            mcpq.leftParty(emc.getId());
                        }

                        var eim = emc.getEventInstance();
                        if (eim != null)
                        {
                            eim.leftParty(emc);
                        }

                        emc.setParty(null);
                        world.updateParty(party.getId(), PartyOperation.EXPEL, emc);

                        emc.updatePartySearchAvailability(true);
                        emc.partyOperationUpdate(party, partyMembers);
                    }
                }
            }
        }
    }
}
