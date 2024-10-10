namespace Application.Core.Game.Commands.Gm5;

public class ShowPacketsCommand : CommandBase
{
    public ShowPacketsCommand() : base(5, "showpackets")
    {
        Description = "Toggle show received packets in console.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        YamlConfig.config.server.USE_DEBUG_SHOW_RCVD_PACKET = !YamlConfig.config.server.USE_DEBUG_SHOW_RCVD_PACKET;
    }
}
