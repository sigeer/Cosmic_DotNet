using constants.net;

namespace Application.Core.Game.Commands.Gm5;

public class SetCommand : CommandBase
{
    public SetCommand() : base(5, "set")
    {
        Description = "Store value in an array, for testing.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        for (int i = 0; i < paramsValue.Length; i++)
        {
            ServerConstants.DEBUG_VALUES[i] = int.Parse(paramsValue[i]);
        }
    }
}
