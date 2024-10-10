using Application.Core.Game.Life;

namespace Application.Core.Game.Commands.Gm6;

public class EraseAllPNpcsCommand : CommandBase
{
    public EraseAllPNpcsCommand() : base(6, "eraseallpnpcs")
    {
        Description = "Remove all player NPCs.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        PlayerNPC.removeAllPlayerNPC();
    }
}
