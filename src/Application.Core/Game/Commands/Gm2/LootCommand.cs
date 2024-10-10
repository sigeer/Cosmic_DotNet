using Application.Core.Game.Maps;
using server.maps;

namespace Application.Core.Game.Commands.Gm2;
public class LootCommand : CommandBase
{
    public LootCommand() : base(2, "loot")
    {
        Description = "Loots all items that belong to you.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var items = c.OnlinedCharacter.getMap().getMapObjectsInRange(c.OnlinedCharacter.getPosition(), double.PositiveInfinity, Arrays.asList(MapObjectType.ITEM));
        foreach (var item in items)
        {
            MapItem mapItem = (MapItem)item;
            if (mapItem.getOwnerId() == c.OnlinedCharacter.getId() || mapItem.getOwnerId() == c.OnlinedCharacter.getPartyId())
            {
                c.OnlinedCharacter.pickupItem(mapItem);
            }
        }

    }
}
