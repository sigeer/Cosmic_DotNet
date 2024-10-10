namespace Application.Core.Game.Commands.Gm2;

public class UnJailCommand : CommandBase
{
    public UnJailCommand() : base(2, "unjail")
    {
        Description = "Free a player from jail.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !unjail <playername>");
            return;
        }

        var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (victim != null && victim.IsOnlined)
        {
            if (victim.getJailExpirationTimeLeft() <= 0)
            {
                player.message("This player is already free.");
                return;
            }
            victim.removeJailExpirationTime();
            victim.message("By lack of concrete proof you are now unjailed. Enjoy freedom!");
            player.message(victim.getName() + " was unjailed.");
        }
        else
        {
            player.message("Player '" + paramsValue[0] + "' could not be found.");
        }
    }
}
