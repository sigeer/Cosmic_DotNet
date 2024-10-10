using net.server;

namespace Application.Core.Game.Commands.Gm6;

public class DCAllCommand : CommandBase
{
    public DCAllCommand() : base(6, "dcall")
    {
        Description = "Disconnect all players (online or logged in).";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        foreach (var world in Server.getInstance().getWorlds())
        {
            foreach (var chr in world.getPlayerStorage().GetAllOnlinedPlayers())
            {
                if (!chr.isGM())
                {
                    chr.getClient().disconnect(false, false);
                }
            }
        }
        player.message("All players successfully disconnected.");
    }
}
