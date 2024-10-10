using client;
using constants.inventory;
using server;

namespace Application.Core.Game.Commands.Gm3;

public class HairCommand : CommandBase
{
    public HairCommand() : base(3, "hair")
    {
        Description = "Change hair of a player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !hair [<playername>] <hairid>");
            return;
        }

        try
        {
            if (paramsValue.Length == 1)
            {
                int itemId = int.Parse(paramsValue[0]);
                if (!ItemConstants.isHair(itemId) || ItemInformationProvider.getInstance().getName(itemId) == null)
                {
                    player.yellowMessage("Hair id '" + paramsValue[0] + "' does not exist.");
                    return;
                }

                player.setHair(itemId);
                player.updateSingleStat(Stat.HAIR, itemId);
                player.equipChanged();
            }
            else
            {
                int itemId = int.Parse(paramsValue[1]);
                if (!ItemConstants.isHair(itemId) || ItemInformationProvider.getInstance().getName(itemId) == null)
                {
                    player.yellowMessage("Hair id '" + paramsValue[1] + "' does not exist.");
                    return;
                }

                var victim = c.getChannelServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
                if (victim != null)
                {
                    victim.setHair(itemId);
                    victim.updateSingleStat(Stat.HAIR, itemId);
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
