using Application.Core.Managers;
using Microsoft.EntityFrameworkCore;

namespace Application.Core.Game.Commands.Gm3;


public class UnBanCommand : CommandBase
{
    public UnBanCommand() : base(3, "unban")
    {
        Description = "Unban a player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !unban <playername>");
            return;
        }

        try
        {
            int aid = CharacterManager.getAccountIdByName(paramsValue[0]);
            using var dbContext = new DBContext();
            dbContext.Accounts.Where(x => x.Id == aid).ExecuteUpdate(x => x.SetProperty(y => y.Banned, -1));

            dbContext.Ipbans.Where(x => x.Aid == aid.ToString()).ExecuteDelete();

            dbContext.Macbans.Where(x => x.Aid == aid.ToString()).ExecuteDelete();
        }
        catch (Exception e)
        {
            log.Error(e.ToString());
            player.message("Failed to unban " + paramsValue[0]);
            return;
        }
        player.message("Unbanned " + paramsValue[0]);
    }
}
