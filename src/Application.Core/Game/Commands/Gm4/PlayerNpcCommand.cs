using Application.Core.Game.Life;

namespace Application.Core.Game.Commands.Gm4;

public class PlayerNpcCommand : CommandBase
{
    public PlayerNpcCommand() : base(4, "playernpc")
    {
        Description = "Spawn a player NPC of an online player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !playernpc <playername>");
            return;
        }

        if (!PlayerNPC.spawnPlayerNPC(player.getMapId(), player.getPosition(), c.getChannelServer().getPlayerStorage().getCharacterByName(paramsValue[0])))
        {
            player.dropMessage(5, "Could not deploy PlayerNPC. Either there's no room available here or depleted out scriptids to use.");
        }
    }
}
