using constants.game;
using net.server;

namespace Application.Core.Game.Commands.Gm5;

/**
 * @author Mist
 * @author Blood (Tochi)
 * @author Ronan
 */
public class IpListCommand : CommandBase
{
    public IpListCommand() : base(5, "iplist")
    {
        Description = "Show IP of all players.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        string str = "Player-IP relation:";

        foreach (var w in Server.getInstance().getWorlds())
        {
            var chars = w.getPlayerStorage().GetAllOnlinedPlayers();

            if (chars.Count > 0)
            {
                str += "\r\n" + GameConstants.WORLD_NAMES[w.getId()] + "\r\n";

                foreach (var chr in chars)
                {
                    str += "  " + chr.getName() + " - " + chr.getClient().getRemoteAddress() + "\r\n";
                }
            }
        }

        c.getAbstractPlayerInteraction().npcTalk(22000, str);
    }

}