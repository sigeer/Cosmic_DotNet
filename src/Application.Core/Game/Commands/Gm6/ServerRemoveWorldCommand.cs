using net.server;
using server;

namespace Application.Core.Game.Commands.Gm6;

public class ServerRemoveWorldCommand : CommandBase
{
    public ServerRemoveWorldCommand() : base(6, "removeworld")
    {
        Description = "Remove a world.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        int rwid = Server.getInstance().getWorldsSize() - 1;
        if (rwid <= 0)
        {
            player.dropMessage(5, "Unable to remove world 0.");
            return;
        }

        ThreadManager.getInstance().newTask(() =>
        {
            if (Server.getInstance().removeWorld())
            {
                if (player.isLoggedinWorld())
                {
                    player.dropMessage(5, "Successfully removed a world. Current world count: " + Server.getInstance().getWorldsSize() + ".");
                }
            }
            else
            {
                if (player.isLoggedinWorld())
                {
                    if (rwid < 0)
                    {
                        player.dropMessage(5, "No registered worlds to remove.");
                    }
                    else
                    {
                        player.dropMessage(5, "Failed to remove world " + rwid + ". Check if there are people currently playing there.");
                    }
                }
            }
        });
    }
}
