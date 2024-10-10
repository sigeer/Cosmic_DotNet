namespace Application.Core.Game.Commands.Gm3;

public class HurtCommand : CommandBase
{
    public HurtCommand() : base(3, "hurt")
    {
        Description = "Nearly kill a player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (victim != null && victim.IsOnlined)
        {
            victim.updateHp(1);
        }
        else
        {
            player.message("Player '" + paramsValue[0] + "' could not be found.");
        }
    }
}
