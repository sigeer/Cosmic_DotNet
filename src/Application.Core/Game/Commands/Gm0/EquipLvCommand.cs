namespace Application.Core.Game.Commands.Gm0;

public class EquipLvCommand : CommandBase
{
    public EquipLvCommand() : base(0, "equiplv")
    {
        Description = "Show levels of all equipped items.";
    }

    public override void Execute(IClient c, string[] paramValues)
    {
        c.OnlinedCharacter.showAllEquipFeatures();
    }
}
