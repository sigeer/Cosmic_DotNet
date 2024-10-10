namespace Application.Core.Game.Commands.Gm3;

public class PosCommand : CommandBase
{
    public PosCommand() : base(3, "pos")
    {
        Description = "Show current position and foothold.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        float xpos = player.getPosition().X;
        float ypos = player.getPosition().Y;
        float fh = player.getMap().getFootholds().findBelow(player.getPosition())!.getId();
        player.dropMessage(6, "Position: (" + xpos + ", " + ypos + ")");
        player.dropMessage(6, "Foothold ID: " + fh);
    }
}
