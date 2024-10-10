namespace Application.Core.Game.Commands.Gm2;

public class ReachCommand : CommandBase
{
    public ReachCommand() : base(2, "reach", "follow", "warpto")
    {
        Description = "Warp to a player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !reach <playername>");
            return;
        }

        var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (victim != null && victim.IsOnlined)
        {
            if (player.getClient().getChannel() != victim.getClient().getChannel())
            {
                player.dropMessage(5, "Player '" + victim.getName() + "' is at channel " + victim.getClient().getChannel() + ".");
            }
            else
            {
                var map = victim.getMap();
                player.saveLocationOnWarp();
                player.forceChangeMap(map, map.findClosestPortal(victim.getPosition()));
            }
        }
        else
        {
            player.dropMessage(6, "Unknown player.");
        }
    }
}
