namespace Application.Core.Game.Commands.Gm2;
public class SetSlotCommand : CommandBase
{
    public SetSlotCommand() : base(2, "setslot")
    {
        Description = "Set amount of inventory slots in all tabs.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !setslot <newlevel>");
            return;
        }

        int slots = (int.Parse(paramsValue[0]) / 4) * 4;
        for (int i = 1; i < 5; i++)
        {
            int curSlots = player.getSlots(i);
            if (slots <= -curSlots)
            {
                continue;
            }

            player.gainSlots(i, slots - curSlots, true);
        }

        player.yellowMessage("Slots updated.");
    }
}
