using client.autoban;
using net.server;
using tools;

namespace Application.Core.Game.Commands.Gm3;

public class IgnoreCommand : CommandBase
{
    public IgnoreCommand() : base(3, "ignore")
    {
        Description = "Toggle ignore a character from auto-ban alerts.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !ignore <ign>");
            return;
        }
        var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (victim == null || !victim.IsOnlined)
        {
            player.message("Player '" + paramsValue[0] + "' could not be found on this world.");
            return;
        }

        bool ignored = AutobanFactory.toggleIgnored(victim.getId());
        player.yellowMessage(victim.getName() + " is " + (ignored ? "now being ignored." : "no longer being ignored."));
        string message_ = player.getName() + (ignored ? " has started ignoring " : " has stopped ignoring ") + victim.getName() + ".";
        Server.getInstance().broadcastGMMessage(c.getWorld(), PacketCreator.serverNotice(5, message_));

    }
}
