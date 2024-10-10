using Application.Core.Managers;
using net.server;

namespace Application.Core.Game.Commands.Gm3;

public class OnlineTwoCommand : CommandBase
{
    public OnlineTwoCommand() : base(3, "online2")
    {
        Description = "Show all online players.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        int total = 0;
        foreach (var ch in Server.getInstance().getChannelsFromWorld(player.getWorld()))
        {
            int size = ch.getPlayerStorage().getAllCharacters().Count;
            total += size;
            string s = "(Channel " + ch.getId() + " Online: " + size + ") : ";
            if (ch.getPlayerStorage().getAllCharacters().Count < 50)
            {
                foreach (var chr in ch.getPlayerStorage().getAllCharacters())
                {
                    s += CharacterManager.makeMapleReadable(chr.getName()) + ", ";
                }
                player.dropMessage(6, s.Substring(0, s.Length - 2));
            }
        }

        //player.dropMessage(6, "There are a total of " + total + " players online.");
        player.showHint("Players online: #e#r" + total + "#k#n.", 300);
    }
}
