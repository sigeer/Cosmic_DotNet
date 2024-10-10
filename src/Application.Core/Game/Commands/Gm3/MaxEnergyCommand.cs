using tools;

namespace Application.Core.Game.Commands.Gm3;

public class MaxEnergyCommand : CommandBase
{
    public MaxEnergyCommand() : base(3, "maxenergy")
    {
        Description = "Set dojo energy to max value.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        c.OnlinedCharacter.setDojoEnergy(10000);
        c.sendPacket(PacketCreator.getEnergy("energy", 10000));
    }
}
