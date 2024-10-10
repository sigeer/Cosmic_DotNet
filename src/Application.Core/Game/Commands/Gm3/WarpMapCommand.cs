namespace Application.Core.Game.Commands.Gm3;

public class WarpMapCommand : CommandBase
{
    public WarpMapCommand() : base(3, "warpmap")
    {
        Description = "Warp all characters on current map to a new map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !warpmap <mapid>");
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

            var characters = player.getMap().getAllPlayers();

            foreach (var victim in characters)
            {
                victim.saveLocationOnWarp();
                victim.changeMap(target, target.getRandomPlayerSpawnpoint());
            }
        }
        catch (Exception ex)
        {
            log.Warning(ex.ToString());
            player.yellowMessage("Map ID " + paramsValue[0] + " is invalid.");
        }
    }
}
