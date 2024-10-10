namespace Application.Core.Game.Commands.Gm0;

public class MapOwnerClaimCommand : CommandBase
{
    public MapOwnerClaimCommand(): base(0, "mylawn")
    {
        Description = "Claim ownership of the current map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        if (c.tryacquireClient())
        {
            try
            {
                var chr = c.OnlinedCharacter;

                if (YamlConfig.config.server.USE_MAP_OWNERSHIP_SYSTEM)
                {
                    if (chr.getEventInstance() == null)
                    {
                        var map = chr.getMap();
                        if (map.countBosses() == 0)
                        {
                            // thanks Conrad for suggesting bosses prevent map leasing
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
