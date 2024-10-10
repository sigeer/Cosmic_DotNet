using net.server;
using server;

namespace Application.Core.Game.Commands.Gm6;

public class ServerAddChannelCommand : CommandBase
{
    public ServerAddChannelCommand() : base(6, "addchannel")
    {
        Description = "Add a new channel to a world.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        if (paramsValue.Length < 1)
        {
            player.dropMessage(5, "Syntax: @addchannel <worldid>");
            return;
        }

        int worldid = int.Parse(paramsValue[0]);

        ThreadManager.getInstance().newTask(() =>
        {
            int chid = Server.getInstance().addChannel(worldid);
            if (player.isLoggedinWorld())
            {
                if (chid >= 0)
                {
                    player.dropMessage(5, "NEW Channel " + chid + " successfully deployed on world " + worldid + ".");
                }
                else
                {
                    if (chid == -3)
                    {
                        player.dropMessage(5, "Invalid worldid detected. Channel creation aborted.");
                    }
                    else if (chid == -2)
                    {
                        player.dropMessage(5, "Reached channel limit on worldid " + worldid + ". Channel creation aborted.");
                    }
                    else if (chid == -1)
                    {
                        player.dropMessage(5, "Error detected when loading the 'world.ini' file. Channel creation aborted.");
                    }
                    else
                    {
                        player.dropMessage(5, "NEW Channel failed to be deployed. Check if the needed port is already in use or other limitations are taking place.");
                    }
                }
            }
        });
    }
}
