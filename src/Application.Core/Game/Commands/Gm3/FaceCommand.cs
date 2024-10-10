using client;
using constants.inventory;
using server;

namespace Application.Core.Game.Commands.Gm3;

public class FaceCommand : CommandBase
{
    public FaceCommand() : base(3, "face")
    {
        Description = "Change face of a player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !face [<playername>] <faceid>");
            return;
        }

        try
        {
            if (paramsValue.Length == 1)
            {
                int itemId = int.Parse(paramsValue[0]);
                if (!ItemConstants.isFace(itemId) || ItemInformationProvider.getInstance().getName(itemId) == null)
                {
                    player.yellowMessage("Face id '" + paramsValue[0] + "' does not exist.");
                    return;
                }

                player.setFace(itemId);
                player.updateSingleStat(Stat.FACE, itemId);
                player.equipChanged();
            }
            else
            {
                int itemId = int.Parse(paramsValue[1]);
                if (!ItemConstants.isFace(itemId) || ItemInformationProvider.getInstance().getName(itemId) == null)
                {
                    player.yellowMessage("Face id '" + paramsValue[1] + "' does not exist.");
                }

                var victim = c.getChannelServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
                if (victim != null)
                {
                    victim.setFace(itemId);
                    victim.updateSingleStat(Stat.FACE, itemId);
                    victim.equipChanged();
                }
                else
                {
                    player.yellowMessage("Player '" + paramsValue[0] + "' has not been found on this channel.");
                }
            }
        }
        catch (Exception e)
        {
            log.Error(e.ToString());
        }

    }
}
