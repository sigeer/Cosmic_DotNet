using net.server.coordinator.session;

namespace Application.Core.Game.Commands.Gm5;

/**
 * @author Ronan
 */
public class ShowSessionsCommand : CommandBase
{
    public ShowSessionsCommand() : base(5, "showsessions")
    {
        Description = "Show online sessions.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        SessionCoordinator.getInstance().printSessionTrace(c);
    }
}
