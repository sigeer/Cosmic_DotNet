using constants.id;
using server.maps;

namespace Application.Core.Game.Commands.Gm0;

public class JoinEventCommand : CommandBase
{
    public JoinEventCommand(): base(0, "joinevent")
    {
        Description = "Join active event.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (!FieldLimit.CANNOTMIGRATE.check(player.getMap().getFieldLimit()))
        {
            var evt = c.getChannelServer().getEvent();
            if (evt != null)
            {
                if (evt.getMapId() != player.getMapId())
                {
                    if (evt.getLimit() > 0)
                    {
                        player.saveLocation("EVENT");

                        if (evt.getMapId() == MapId.EVENT_COCONUT_HARVEST || evt.getMapId() == MapId.EVENT_SNOWBALL_ENTRANCE)
                        {
                            player.setTeam(evt.getLimit() % 2);
                        }

                        evt.minusLimit();

                        player.saveLocationOnWarp();
                        player.changeMap(evt.getMapId());
                    }
                    else
                    {
                        player.dropMessage(5, "The limit of players for the event has already been reached.");
                    }
                }
                else
                {
                    player.dropMessage(5, "You are already in the event.");
                }
            }
            else
            {
                player.dropMessage(5, "There is currently no event in progress.");
            }
        }
        else
        {
            player.dropMessage(5, "You are currently in a map where you can't join an event.");
        }
    }
}
