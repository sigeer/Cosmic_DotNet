namespace Application.Core.Game.Commands.Gm3;

public class KillMapCommand : CommandBase
{
    public KillMapCommand() : base(3, "killmap")
    {
        Description = "Kill all players in the map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        foreach (var mch in player.getMap().getCharacters())
        {
            mch.updateHp(0);
        }
    }
}
