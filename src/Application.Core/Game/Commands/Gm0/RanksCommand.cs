using constants.id;
using net.server;
using net.server.guild;

namespace Application.Core.Game.Commands.Gm0;

public class RanksCommand : CommandBase
{
    public RanksCommand(): base(0, "ranks")
    {
        Description = "Show player rankings.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        var worldRanking = Server.getInstance().getWorldPlayerRanking(player.getWorld());
        player.sendPacket(GuildPackets.showPlayerRanks(NpcId.MAPLE_ADMINISTRATOR, worldRanking));
    }
}
