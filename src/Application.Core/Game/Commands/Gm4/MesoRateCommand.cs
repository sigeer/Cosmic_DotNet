using tools;

namespace Application.Core.Game.Commands.Gm4;

public class MesoRateCommand : CommandBase
{
    public MesoRateCommand() : base(4, "mesorate")
    {
        Description = "Set world meso rate.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !mesorate <newrate>");
            return;
        }

        if (!int.TryParse(paramsValue[0], out var d))
            return;

        int mesorate = Math.Max(d, 1);
        c.getWorldServer().setMesoRate(mesorate);
        c.getWorldServer().broadcastPacket(PacketCreator.serverNotice(6, "[Rate] Meso Rate has been changed to " + mesorate + "x."));
    }
}
