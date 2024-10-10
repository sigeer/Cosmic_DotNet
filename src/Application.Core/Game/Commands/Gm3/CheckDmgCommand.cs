using client;

namespace Application.Core.Game.Commands.Gm3;

public class CheckDmgCommand : CommandBase
{
    public CheckDmgCommand() : base(3, "checkdmg")
    {
        Description = "Show stats and damage of a player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (victim != null && victim.IsOnlined)
        {
            int maxBase = victim.calculateMaxBaseDamage(victim.getTotalWatk());
            var watkBuff = victim.getBuffedValue(BuffStat.WATK) ?? 0;
            var matkBuff = victim.getBuffedValue(BuffStat.MATK) ?? 0;
            int blessing = victim.getSkillLevel(10000000 * player.getJobType() + 12);

            player.dropMessage(5, "Cur Str: " + victim.getTotalStr() + " Cur Dex: " + victim.getTotalDex() + " Cur Int: " + victim.getTotalInt() + " Cur Luk: " + victim.getTotalLuk());
            player.dropMessage(5, "Cur WATK: " + victim.getTotalWatk() + " Cur MATK: " + victim.getTotalMagic());
            player.dropMessage(5, "Cur WATK Buff: " + watkBuff + " Cur MATK Buff: " + matkBuff + " Cur Blessing Level: " + blessing);
            player.dropMessage(5, victim.getName() + "'s maximum base damage (before skills) is " + maxBase);
        }
        else
        {
            player.message("Player '" + paramsValue[0] + "' could not be found on this world.");
        }
    }
}
