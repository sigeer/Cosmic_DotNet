namespace Application.Core.Game.Commands.Gm3;

public class ClosePortalCommand : CommandBase
{
    public ClosePortalCommand() : base(3, "closeportal")
    {
        Description = "Close a portal.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !closeportal <portalid>");
            return;
        }
        var portal = player.getMap().getPortal(paramsValue[0]);
        if (portal == null)
        {
            player.yellowMessage("invalid portalid");
            return;
        }
        portal.setPortalState(false);
    }
}
