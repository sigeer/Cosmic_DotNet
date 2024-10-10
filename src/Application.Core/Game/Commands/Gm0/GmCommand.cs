using Application.Core.Managers;
using net.server;
using tools;

namespace Application.Core.Game.Commands.Gm0;


public class GmCommand : CommandBase
{
    public GmCommand() : base(0, "gm")
    {
        Description = "Send a message to the game masters.";
    }

    readonly static string[] tips = {
                "Please only use @gm in emergencies or to report somebody.",
                "To report a bug or make a suggestion, use the forum.",
                "Please do not use @gm to ask if a GM is online.",
                "Do not ask if you can receive help, just state your issue.",
                "Do not say 'I have a bug to report', just state it.",
        };
    public override void Execute(IClient c, string[] paramValues)
    {
        var player = c.OnlinedCharacter;
        if (paramValues.Length < 1 || paramValues[0].Length < 3)
        { // #goodbye 'hi'
            player.dropMessage(5, "Your message was too short. Please provide as much detail as possible.");
            return;
        }
        string message = player.getLastCommandMessage();
        Server.getInstance().broadcastGMMessage(c.getWorld(), PacketCreator.sendYellowTip("[GM Message]:" + CharacterManager.makeMapleReadable(player.getName()) + ": " + message));
        Server.getInstance().broadcastGMMessage(c.getWorld(), PacketCreator.serverNotice(1, message));
        log.Information("{CharacterName}: {Message}", CharacterManager.makeMapleReadable(player.getName()), message);
        player.dropMessage(5, "Your message '" + message + "' was sent to GMs.");
        player.dropMessage(5, tips[Randomizer.nextInt(tips.Length)]);
    }
}
