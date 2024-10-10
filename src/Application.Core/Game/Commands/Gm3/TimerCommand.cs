using tools;

namespace Application.Core.Game.Commands.Gm3;

public class TimerCommand : CommandBase
{
    public TimerCommand() : base(3, "timer")
    {
        Description = "Set timer on a player in current map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 2)
        {
            player.yellowMessage("Syntax: !timer <playername> <seconds>|remove");
            return;
        }

        var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (victim != null && victim.IsOnlined)
        {
            if (paramsValue[1].Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                victim.sendPacket(PacketCreator.removeClock());
            }
            else
            {
                try
                {
                    victim.sendPacket(PacketCreator.getClock(int.Parse(paramsValue[1])));
                }
                catch (FormatException e)
                {
                    log.Error(e.ToString());
                    player.yellowMessage("Syntax: !timer <playername> <seconds>|remove");
                }
            }
        }
        else
        {
            player.message("Player '" + paramsValue[0] + "' could not be found.");
        }
    }
}
