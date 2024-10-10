using net.server;
using server;

namespace Application.Core.Game.Commands.Gm6;

public class ServerRemoveChannelCommand : CommandBase
{
    public ServerRemoveChannelCommand() : base(6, "removechannel")
    {
        Description = "Remove channel from a world.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        if (paramsValue.Length < 1)
        {
            player.dropMessage(5, "Syntax: @removechannel <worldid>");
            return;
        }

        int worldId = int.Parse(paramsValue[0]);
        ThreadManager.getInstance().newTask(() =>
        {
            if (Server.getInstance().removeChannel(worldId))
            {
                if (player.isLoggedinWorld())
                {
                    player.dropMessage(5, "Successfully removed a channel on World " + worldId + ". Current channel count: " + Server.getInstance().getWorld(worldId).getChannelsSize() + ".");
                }
            }
            else
            {
                if (player.isLoggedinWorld())
                {
                    player.dropMessage(5, "Failed to remove last Channel on world " + worldId + ". Check if either that world exists or there are people currently playing there.");
                }
            }
        });
    }
}
