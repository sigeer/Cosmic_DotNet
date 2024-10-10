namespace Application.Core.Game.Commands.Gm3;

public class ChatCommand : CommandBase
{
    public ChatCommand() : base(3, "togglewhitechat")
    {
        Description = "Toggle white GM chat.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        player.toggleWhiteChat();
        player.message("Your chat is now " + (player.getWhiteChat() ? " white" : "normal") + ".");
    }
}
