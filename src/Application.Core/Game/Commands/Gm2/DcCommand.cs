namespace Application.Core.Game.Commands.Gm2;

public class DcCommand : CommandBase
{
    public DcCommand() : base(2, "dc")
    {
        Description = "Disconnect a player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !dc <playername>");
            return;
        }

        var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (victim == null || !victim.IsOnlined)
        {
            victim = c.getChannelServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
            if (victim == null)
            {
                victim = player.getMap().getCharacterByName(paramsValue[0]);
                if (victim != null)
                {
                    try
                    {//sometimes bugged because the map = null
                        victim.getClient().disconnect(true, false);
                        player.getMap().removePlayer(victim);
                    }
                    catch (Exception e)
                    {
                        log.Error(e.ToString());
                    }
                }
                else
                {
                    return;
                }
            }
        }
        if (player.gmLevel() < victim.gmLevel())
        {
            victim = player;
        }
        victim.getClient().disconnect(false, false);
    }
}
