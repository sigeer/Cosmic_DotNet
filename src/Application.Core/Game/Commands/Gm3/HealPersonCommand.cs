namespace Application.Core.Game.Commands.Gm3;

public class HealPersonCommand : CommandBase
{
    public HealPersonCommand() : base(3, "healperson")
    {
        Description = "Heal all HP/MP of a player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (victim != null && victim.IsOnlined)
        {
            victim.healHpMp();
        }
        else
        {
            player.message("Player '" + paramsValue[0] + "' could not be found.");
        }
    }
}
