namespace Application.Core.Game.Commands.Gm0;

public class ToggleExpCommand : CommandBase
{
    public ToggleExpCommand() : base(0, "toggleexp")
    {
        Description = "Toggle enable/disable all exp gain.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        if (c.tryacquireClient())
        {
            try
            {
                c.OnlinedCharacter.toggleExpGain();  // Vcoc's idea
            }
            finally
            {
                c.releaseClient();
            }
        }
    }
}
