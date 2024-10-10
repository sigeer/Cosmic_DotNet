using Application.Core.Managers;

namespace Application.Core.Game.Commands.Gm4;

public class SetEqStatCommand : CommandBase
{
    public SetEqStatCommand() : base(4, "seteqstat")
    {
        Description = "Set stats of all equips in inventory.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !seteqstat <stat value> [<spdjmp value>]");
            return;
        }

        if (!short.TryParse(paramsValue[0], out short newStat))
        {
            player.yellowMessage("Invalid stat value.");
            return;
        }

        newStat = Math.Max((short)0, newStat);
        short newSpdJmp = 0;

        if (paramsValue.Length >= 2 && short.TryParse(paramsValue[2], out newSpdJmp))
            newSpdJmp = Math.Max((short)0, newSpdJmp);

        ItemManager.UpdateEquip(player, newStat, newSpdJmp);
    }
}
