namespace Application.Core.Game.Commands.Gm3;

public class MuteMapCommand : CommandBase
{
    public MuteMapCommand() : base(3, "mutemap")
    {
        Description = "Toggle mute players in the map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (player.getMap().isMuted())
        {
            player.getMap().setMuted(false);
            player.dropMessage(5, "The map you are in has been un-muted.");
        }
        else
        {
            player.getMap().setMuted(true);
            player.dropMessage(5, "The map you are in has been muted.");
        }
    }
}
