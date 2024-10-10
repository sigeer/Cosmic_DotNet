using Application.Core.Game.Life;

namespace Application.Core.Game.Commands.Gm4;

public class PlayerNpcRemoveCommand : CommandBase
{
    public PlayerNpcRemoveCommand() : base(4, "playernpcremove")
    {
        Description = "Remove a \"lv 200\" player NPC.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !playernpcremove <playername>");
            return;
        }
        PlayerNPC.removePlayerNPC(c.getChannelServer().getPlayerStorage().getCharacterByName(paramsValue[0]));
    }
}
