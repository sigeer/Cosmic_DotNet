using Application.Core.Managers;
using net.server;

namespace Application.Core.Game.Commands.Gm0;

public class OnlineCommand : CommandBase
{
    public OnlineCommand(): base(0, "online")
    {
        Description = "Show all online players.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        foreach (var ch in Server.getInstance().getChannelsFromWorld(player.World))
        {
            player.yellowMessage("Players in Channel " + ch.getId() + ":");
            foreach (var chr in ch.getPlayerStorage().getAllCharacters())
            {
                if (!chr.isGM())
                {
                    player.message(" >> " + CharacterManager.makeMapleReadable(chr.getName()) + " is at " + chr.getMap().getMapName() + ".");
                }
            }
        }
    }
}
