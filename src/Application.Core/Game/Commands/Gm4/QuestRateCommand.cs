using tools;

namespace Application.Core.Game.Commands.Gm4;
public class QuestRateCommand : CommandBase
{
    public QuestRateCommand() : base(4, "questrate")
    {
        Description = "Set world quest rate.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !questrate <newrate>");
            return;
        }

        if (!int.TryParse(paramsValue[0], out var d))
            return;

        int questrate = Math.Max(d, 1);
        c.getWorldServer().QuestRate  = questrate;
        c.getWorldServer().broadcastPacket(PacketCreator.serverNotice(6, "[Rate] Quest Rate has been changed to " + questrate + "x."));

    }
}
