using net.server.coordinator.login;

namespace Application.Core.Game.Commands.Gm0;

public class EnableAuthCommand : CommandBase
{
    public EnableAuthCommand() : base(0, "enableauth")
    {
    }

    public override void Execute(IClient c, string[] paramValues)
    {
        if (c.tryacquireClient())
        {
            try
            {
                LoginBypassCoordinator.getInstance().unregisterLoginBypassEntry(c.getHwid(), c.getAccID());
            }
            finally
            {
                c.releaseClient();
            }
        }
    }
}
