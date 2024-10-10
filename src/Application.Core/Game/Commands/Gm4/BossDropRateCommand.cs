using tools;

namespace Application.Core.Game.Commands.Gm4;

public class BossDropRateCommand : CommandBase
{
    public BossDropRateCommand() : base(4, "bossdroprate")
    {
        Description = "Set world boss drop rate.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !bossdroprate <newrate>");
            return;
        }

        if (int.TryParse(paramsValue[0], out var d))
        {
            int bossdroprate = Math.Max(d, 1);
            c.getWorldServer().BossDropRate = bossdroprate;
            c.getWorldServer().broadcastPacket(PacketCreator.serverNotice(6, "[Rate] Boss Drop Rate has been changed to " + bossdroprate + "x."));
        }

    }
}
