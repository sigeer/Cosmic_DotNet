using constants.id;

namespace Application.Core.Game.Commands.Gm2;

public class JailCommand : CommandBase
{
    public JailCommand() : base(2, "jail")
    {
        Description = "Move a player to the jail.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !jail <playername> [<minutes>]");
            return;
        }

        int minutesJailed = 5;
        if (paramsValue.Length >= 2)
        {
            if (!int.TryParse(paramsValue[1], out minutesJailed))
            {
                player.yellowMessage("Syntax: !jail <playername> [<minutes>]");
                return;
            }
        }

        var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (victim != null && victim.IsOnlined)
        {
            victim.addJailExpirationTime(minutesJailed * 60 * 1000);

            if (victim.getMapId() != MapId.JAIL)
            {
                // those gone to jail won't be changing map anyway
                var target = c.getChannelServer().getMapFactory().getMap(MapId.JAIL);
                var targetPortal = target.getPortal(0);
                victim.saveLocationOnWarp();
                victim.changeMap(target, targetPortal);
                player.message(victim.getName() + " was jailed for " + minutesJailed + " minutes.");
            }
            else
            {
                player.message(victim.getName() + "'s time in jail has been extended for " + minutesJailed + " minutes.");
            }

        }
        else
        {
            player.message("Player '" + paramsValue[0] + "' could not be found.");
        }
    }
}
