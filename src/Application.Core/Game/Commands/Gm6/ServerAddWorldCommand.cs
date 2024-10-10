using net.server;
using server;

namespace Application.Core.Game.Commands.Gm6;

public class ServerAddWorldCommand : CommandBase
{
    public ServerAddWorldCommand() : base(6, "addworld")
    {
        Description = "Add a new world.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        ThreadManager.getInstance().newTask(() =>
        {
            int wid = Server.getInstance().addWorld();

            if (player.isLoggedinWorld())
            {
                if (wid >= 0)
                {
                    player.dropMessage(5, "NEW World " + wid + " successfully deployed.");
                }
                else
                {
                    if (wid == -2)
                    {
                        player.dropMessage(5, "Error detected when loading the 'world.ini' file. World creation aborted.");
                    }
                    else
                    {
                        player.dropMessage(5, "NEW World failed to be deployed. Check if needed ports are already in use or maximum world count has been reached.");
                    }
                }
            }
        });
    }
}
