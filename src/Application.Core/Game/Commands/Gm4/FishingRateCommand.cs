using tools;

namespace Application.Core.Game.Commands.Gm4;

public class FishingRateCommand : CommandBase
{
    public FishingRateCommand() : base(4, "fishrate")
    {
        Description = "Set fishing rate.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !fishrate <newrate>");
            return;
        }

        if (!int.TryParse(paramsValue[0], out var d))
            return;

        int fishrate = Math.Max(d, 1);
        c.getWorldServer().FishingRate = fishrate;
        c.getWorldServer().broadcastPacket(PacketCreator.serverNotice(6, "[Rate] Fishing Rate has been changed to " + fishrate + "x."));
    }
}
