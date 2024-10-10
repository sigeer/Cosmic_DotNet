using net.server;
using System.Net;
using tools;

namespace Application.Core.Game.Commands.Gm6;


public class WarpWorldCommand : CommandBase
{
    public WarpWorldCommand() : base(6, "warpworld")
    {
        Description = "Warp to a different world.";
    }

    public override void Execute(IClient c, string[] paramValues)
    {
        var player = c.OnlinedCharacter;
        if (paramValues.Length < 1)
        {
            player.yellowMessage("Syntax: !warpworld <worldid>");
            return;
        }

        Server server = Server.getInstance();
        byte worldb = byte.Parse(paramValues[0]);
        if (worldb <= (server.getWorldsSize() - 1))
        {
            try
            {
                string[] socket = server.getInetSocket(c, worldb, c.getChannel());
                c.getWorldServer().removePlayer(player);
                player.getMap().removePlayer(player);//LOL FORGOT THIS ><
                player.setSessionTransitionState();
                player.setWorld(worldb);
                player.saveCharToDB();//To set the new world :O (true because else 2 player instances are created, one in both worlds)
                c.sendPacket(PacketCreator.getChannelChange(IPAddress.Parse(socket[0]), int.Parse(socket[1])));
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                player.message("Unexpected error when changing worlds, are you sure the world you are trying to warp to has the same amount of channels?");
            }

        }
        else
        {
            player.message("Invalid world; highest number available: " + (server.getWorldsSize() - 1));
        }
    }
}
