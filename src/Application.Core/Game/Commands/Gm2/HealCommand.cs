namespace Application.Core.Game.Commands.Gm2;

public class HealCommand : CommandBase
{
    public HealCommand() : base(2, "heal")
    {
        Description = "Fully heal your HP/MP.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        player.healHpMp();
    }

}
