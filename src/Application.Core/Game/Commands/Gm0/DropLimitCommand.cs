namespace Application.Core.Game.Commands.Gm0;


public class DropLimitCommand : CommandBase
{
    public DropLimitCommand() : base(0, "droplimit")
    {

    }
    public override void Execute(IClient c, string[] paramValues)
    {
        int dropCount = c.OnlinedCharacter.getMap().getDroppedItemCount();
        if (((float)dropCount) / YamlConfig.config.server.ITEM_LIMIT_ON_MAP < 0.75f)
        {
            c.OnlinedCharacter.showHint("Current drop count: #b" + dropCount + "#k / #e" + YamlConfig.config.server.ITEM_LIMIT_ON_MAP + "#n", 300);
        }
        else
        {
            c.OnlinedCharacter.showHint("Current drop count: #r" + dropCount + "#k / #e" + YamlConfig.config.server.ITEM_LIMIT_ON_MAP + "#n", 300);
        }

    }
}
