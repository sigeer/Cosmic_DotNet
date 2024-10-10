using tools;

namespace Application.Core.Game.Commands.Gm4;

public class TravelRateCommand : CommandBase
{
    public TravelRateCommand() : base(4, "travelrate")
    {
        Description = "Set world travel rate.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !travelrate <newrate>");
            return;
        }

        if (!int.TryParse(paramsValue[0], out int d))
        {
            player.yellowMessage("Syntax: newrate invalid");
            return;
        }

        int travelrate = Math.Max(d, 1);
        c.getWorldServer().TravelRate = travelrate;
        c.getWorldServer().broadcastPacket(PacketCreator.serverNotice(6, "[Rate] Travel Rate has been changed to " + travelrate + "x."));
    }
}
