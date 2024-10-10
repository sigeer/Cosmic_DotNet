using net.server;

namespace Application.Core.Game.Commands.Gm2;

public class SummonCommand : CommandBase
{
    public SummonCommand() : base(2, "warphere", "summon")
    {
        Description = "Move a player to your location.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage($"Syntax: !{Syntax[0]} <playername>");
            return;
        }

        var victim = c.getChannelServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (victim == null)
        {
            //If victim isn't on current channel, loop all channels on current world.

            foreach (var ch in Server.getInstance().getChannelsFromWorld(c.getWorld()))
            {
                victim = ch.getPlayerStorage().getCharacterByName(paramsValue[0]);
                if (victim != null)
                {
                    break;//We found the person, no need to continue the loop.
                }
            }
        }
        if (victim != null)
        {
            if (!victim.isLoggedinWorld())
            {
                player.dropMessage(6, "Player currently not logged in or unreachable.");
                return;
            }

            if (player.getClient().getChannel() != victim.getClient().getChannel())
            {//And then change channel if needed.
                victim.dropMessage("Changing channel, please wait a moment.");
                victim.getClient().changeChannel(player.getClient().getChannel());
            }

            try
            {
                for (int i = 0; i < 7; i++)
                {
                    // poll for a while until the player reconnects
                    if (victim.isLoggedinWorld())
                    {
                        break;
                    }
                    Thread.Sleep(1777);
                }
            }
            catch (ThreadInterruptedException e)
            {
                log.Error(e.ToString());
            }

            var map = player.getMap();
            victim.saveLocationOnWarp();
            victim.forceChangeMap(map, map.findClosestPortal(player.getPosition()));
        }
        else
        {
            player.dropMessage(6, "Unknown player.");
        }
    }
}
