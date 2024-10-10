namespace Application.Core.Game.Commands.Gm3;

public class ReloadMapCommand : CommandBase
{
    public ReloadMapCommand() : base(3, "reloadmap")
    {
        Description = "Reload the map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        var newMap = c.getChannelServer().getMapFactory().resetMap(player.getMapId());
        int callerid = c.OnlinedCharacter.getId();

        var characters = player.getMap().getAllPlayers();

        foreach (var chr in characters)
        {
            chr.saveLocationOnWarp();
            chr.changeMap(newMap);
            if (chr.getId() != callerid)
            {
                chr.dropMessage("You have been relocated due to map reloading. Sorry for the inconvenience.");
            }
        }
        newMap.respawn();
    }
}
