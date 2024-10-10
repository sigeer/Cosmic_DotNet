namespace Application.Core.Game.Commands.Gm3;

public class HealMapCommand : CommandBase
{
    public HealMapCommand() : base(3, "healmap")
    {
        Description = "Heal all HP/MP of all players in the map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        foreach (var mch in player.getMap().getCharacters())
        {
            if (mch != null)
            {
                mch.healHpMp();
            }
        }
    }
}
