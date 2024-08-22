/*
    This file is part of the HeavenMS MapleStory Server, commands OdinMS-based
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

/*
   @Author: Ronan
*/


using server.maps;

namespace client.command.commands.gm0;

public class MapOwnerClaimCommand : Command
{
    public MapOwnerClaimCommand()
    {
        setDescription("Claim ownership of the current map.");
    }

    public override void execute(Client c, string[] paramsValue)
    {
        if (c.tryacquireClient())
        {
            try
            {
                Character chr = c.getPlayer();

                if (YamlConfig.config.server.USE_MAP_OWNERSHIP_SYSTEM)
                {
                    if (chr.getEventInstance() == null)
                    {
                        MapleMap map = chr.getMap();
                        if (map.countBosses() == 0)
                        {   // thanks Conrad for suggesting bosses prevent map leasing
                            var ownedMap = chr.getOwnedMap();  // thanks Conrad for suggesting not unlease a map as soon as player exits it
                            if (ownedMap != null)
                            {
                                ownedMap.unclaimOwnership(chr);

                                if (map == ownedMap)
                                {
                                    chr.dropMessage(5, "This lawn is now free real estate.");
                                    return;
                                }
                            }

                            if (map.claimOwnership(chr))
                            {
                                chr.dropMessage(5, "You have leased this lawn for a while, until you leave here or after 1 minute of inactivity.");
                            }
                            else
                            {
                                chr.dropMessage(5, "This lawn has already been leased by a player.");
                            }
                        }
                        else
                        {
                            chr.dropMessage(5, "This lawn is currently under a boss siege.");
                        }
                    }
                    else
                    {
                        chr.dropMessage(5, "This lawn cannot be leased.");
                    }
                }
                else
                {
                    chr.dropMessage(5, "Feature unavailable.");
                }
            }
            finally
            {
                c.releaseClient();
            }
        }
    }
}
