using tools;

namespace Application.Core.Game.Commands.Gm2;

public class UnBugCommand : CommandBase
{
    public UnBugCommand() : base(2, "unbug")
    {
        Description = "Unbug self.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        c.OnlinedCharacter.getMap().broadcastMessage(PacketCreator.enableActions());
    }
}
