using constants.game;
using constants.id;
using tools;

namespace Application.Core.Game.Commands.Gm3;

public class MusicCommand : CommandBase
{
    public MusicCommand() : base(3, "music")
    {
        Description = "Play a song.";
    }

    private static string getSongList()
    {
        string songList = "Song:\r\n";
        foreach (string s in GameConstants.GAME_SONGS)
        {
            songList += ("  " + s + "\r\n");
        }

        return songList;
    }

    public override void Execute(IClient c, string[] paramsValue)
    {

        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            string sendMsgTemp = "";

            sendMsgTemp += "Syntax: #r!music <song>#k\r\n\r\n";
            sendMsgTemp += getSongList();

            c.sendPacket(PacketCreator.getNPCTalk(NpcId.BILLY, 0, sendMsgTemp, "00 00", 0));
            return;
        }

        string song = player.getLastCommandMessage();
        foreach (string s in GameConstants.GAME_SONGS)
        {
            if (s.Equals(song, StringComparison.OrdinalIgnoreCase))
            {    // thanks Masterrulax for finding an issue here
                player.getMap().broadcastMessage(PacketCreator.musicChange(s));
                player.yellowMessage("Now playing song " + s + ".");
                return;
            }
        }

        string sendMsg = "";
        sendMsg += "Song not found, please enter a song below.\r\n\r\n";
        sendMsg += getSongList();

        c.sendPacket(PacketCreator.getNPCTalk(NpcId.BILLY, 0, sendMsg, "00 00", 0));
    }
}
