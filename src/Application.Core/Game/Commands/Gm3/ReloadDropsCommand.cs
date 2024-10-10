using server.life;

namespace Application.Core.Game.Commands.Gm3;

public class ReloadDropsCommand : CommandBase
{
    public ReloadDropsCommand() : base(3, "reloaddrops")
    {
        Description = "Reload all drop data.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        MonsterInformationProvider.getInstance().clearDrops();
        player.dropMessage(5, "Reloaded Drops");
    }
}
