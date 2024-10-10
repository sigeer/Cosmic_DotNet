namespace Application.Core.Game.Commands.Gm3;
public class WarpAreaCommand : CommandBase
{
    public WarpAreaCommand() : base(3, "warparea")
    {
        Description = "Warp all nearby players to a new map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !warparea <mapid>");
            return;
        }

        try
        {
            var target = c.getChannelServer().getMapFactory().getMap(int.Parse(paramsValue[0]));
            if (target == null)
            {
                player.yellowMessage("Map ID " + paramsValue[0] + " is invalid.");
                return;
            }

            Point pos = player.getPosition();

            var characters = player.getMap().getAllPlayers();

            foreach (var victim in characters)
            {
                if (victim.getPosition().distanceSq(pos) <= 50000)
                {
                    victim.saveLocationOnWarp();
                    victim.changeMap(target, target.getRandomPlayerSpawnpoint());
                }
            }
        }
        catch (Exception e)
        {
            log.Error(e.ToString());
            player.yellowMessage("Map ID " + paramsValue[0] + " is invalid.");
        }
    }
}
