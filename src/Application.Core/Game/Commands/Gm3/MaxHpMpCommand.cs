namespace Application.Core.Game.Commands.Gm3;

public class MaxHpMpCommand : CommandBase
{
    public MaxHpMpCommand() : base(3, "maxhpmp")
    {
        Description = "Set base HP/MP of a player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        var victim = player;

        int statUpdate = 1;
        if (paramsValue.Length >= 2)
        {
            victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
            statUpdate = int.Parse(paramsValue[1]);
        }
        else if (paramsValue.Length == 1)
        {
            statUpdate = int.Parse(paramsValue[0]);
        }
        else
        {
            player.yellowMessage("Syntax: !maxhpmp [<playername>] <value>");
        }

        if (victim != null && victim.IsOnlined)
        {
            int extraHp = victim.getCurrentMaxHp() - victim.getClientMaxHp();
            int extraMp = victim.getCurrentMaxMp() - victim.getClientMaxMp();
            statUpdate = Math.Max(1 + Math.Max(extraHp, extraMp), statUpdate);

            int maxhpUpdate = statUpdate - extraHp;
            int maxmpUpdate = statUpdate - extraMp;
            victim.updateMaxHpMaxMp(maxhpUpdate, maxmpUpdate);
        }
        else
        {
            player.message("Player '" + paramsValue[0] + "' could not be found on this world.");
        }
    }
}
