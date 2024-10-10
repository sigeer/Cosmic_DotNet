namespace Application.Core.Game.Commands.Gm5;

public class ShowMoveLifeCommand : CommandBase
{
    public ShowMoveLifeCommand() : base(5, "showmovelife")
    {
        Description = "Toggle show move life in console.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        YamlConfig.config.server.USE_DEBUG_SHOW_RCVD_MVLIFE = !YamlConfig.config.server.USE_DEBUG_SHOW_RCVD_MVLIFE;
    }
}
