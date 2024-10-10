namespace Application.Core.Game.Commands.Gm3;

public class OpenPortalCommand : CommandBase
{
    public OpenPortalCommand() : base(3, "openportal")
    {
        Description = "Open a portal on the map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !openportal <portalid>");
            return;
        }
        player.getMap().getPortal(paramsValue[0])?.setPortalState(true);
    }
}
