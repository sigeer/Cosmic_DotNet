using tools;

namespace Application.Core.Game.Commands.Gm3;

public class TimerMapCommand : CommandBase
{
    public TimerMapCommand() : base(3, "timermap")
    {
        Description = "Set timer on all players in current map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !timermap <seconds>|remove");
            return;
        }

        if (paramsValue[0].Equals("remove", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var victim in player.getMap().getCharacters())
            {
                victim.sendPacket(PacketCreator.removeClock());
            }
        }
        else
        {
            try
            {
                int seconds = int.Parse(paramsValue[0]);
                foreach (var victim in player.getMap().getCharacters())
                {
                    victim.sendPacket(PacketCreator.getClock(seconds));
                }
            }
            catch (FormatException e)
            {
                log.Warning(e.ToString());
                player.yellowMessage("Syntax: !timermap <seconds>|remove");
            }
        }
    }
}
