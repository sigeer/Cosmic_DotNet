namespace Application.Core.Game.Commands.Gm2;

/**
 * @author Ronan
 */
public class GachaListCommand : CommandBase
{
    public GachaListCommand() : base(2, "gachalist")
    {
        Description = "Show gachapon rewards.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        c.getAbstractPlayerInteraction().openNpc(10000, "gachaponInfo");
    }
}
