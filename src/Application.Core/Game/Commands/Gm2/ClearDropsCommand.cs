namespace Application.Core.Game.Commands.Gm2;

public class ClearDropsCommand : CommandBase
{
    public ClearDropsCommand() : base(2, "cleardrops")
    {
        Description = "Clear drops by player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        player.getMap().clearDrops(player);
        player.dropMessage(5, "Cleared dropped items");
    }
}
