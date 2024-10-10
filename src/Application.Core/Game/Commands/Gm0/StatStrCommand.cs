namespace Application.Core.Game.Commands.Gm0;

public class StatStrCommand : CommandBase
{
    public StatStrCommand() : base(0, "str")
    {
        Description = "Assign AP into STR.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        int remainingAp = player.getRemainingAp();
        int amount;
        if (paramsValue.Length > 0)
        {
            try
            {
                amount = Math.Min(int.Parse(paramsValue[0]), remainingAp);
            }
            catch (Exception e)
            {
                log.Warning(e.ToString());
                player.dropMessage("That is not a valid number!");
                return;
            }
        }
        else
        {
            amount = Math.Min(remainingAp, YamlConfig.config.server.MAX_AP - player.getStr());
        }

        if (!player.assignStr(Math.Max(amount, 0)))
        {
            player.dropMessage("Please make sure your AP is not over " + YamlConfig.config.server.MAX_AP + " and you have enough to distribute.");
        }
    }
}
