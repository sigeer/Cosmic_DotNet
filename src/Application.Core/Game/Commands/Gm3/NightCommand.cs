namespace Application.Core.Game.Commands.Gm3;

public class NightCommand : CommandBase
{
    public NightCommand() : base(3, "night")
    {
        Description = "Set sky background to black.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        player.getMap().broadcastNightEffect();
        player.yellowMessage("Done.");
    }
}
