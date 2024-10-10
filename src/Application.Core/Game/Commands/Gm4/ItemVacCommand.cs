using server.maps;

namespace Application.Core.Game.Commands.Gm4;

public class ItemVacCommand : CommandBase
{
    public ItemVacCommand() : base(4, "itemvac")
    {
        Description = "Loot all drops on the map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        var list = player.getMap().getMapObjectsInRange(player.getPosition(), double.PositiveInfinity, Arrays.asList(MapObjectType.ITEM));
        foreach (var item in list)
        {
            player.pickupItem(item);
        }
    }
}
