using tools;

namespace Application.Core.Game.Commands.Gm4;

public class ExpRateCommand : CommandBase
{
    public ExpRateCommand() : base(4, "exprate")
    {
        Description = "Set world exp rate.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !exprate <newrate>");
            return;
        }

        if (!int.TryParse(paramsValue[0], out var d))
            return;

        int exprate = Math.Max(d, 1);
        c.getWorldServer().setExpRate(exprate);
        c.getWorldServer().broadcastPacket(PacketCreator.serverNotice(6, "[Rate] Exp Rate has been changed to " + exprate + "x."));
    }
}
