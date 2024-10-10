namespace Application.Core.Game.Commands.Gm2;

public class LevelProCommand : CommandBase
{
    public LevelProCommand() : base(2, "levelpro")
    {
        Description = "Set your level, one by one.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !levelpro <newlevel>");
            return;
        }

        if (!int.TryParse(paramsValue[0], out var newlevel))
        {
            player.yellowMessage("Syntax: <newlevel> invalid");
            return;
        }

        var targetLevel = Math.Min(player.getMaxClassLevel(), newlevel);
        while (player.getLevel() < targetLevel)
        {
            player.levelUp(false);
        }
    }
}
