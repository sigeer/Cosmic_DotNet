using server.quest;

namespace Application.Core.Game.Commands.Gm6;

public class ClearQuestCacheCommand : CommandBase
{
    public ClearQuestCacheCommand() : base(6, "clearquestcache")
    {
        Description = "Clear all quest cache.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        Quest.clearCache();
        player.dropMessage(5, "Quest Cache Cleared.");
    }
}
