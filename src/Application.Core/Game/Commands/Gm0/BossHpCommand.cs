namespace Application.Core.Game.Commands.Gm0;

public class BossHpCommand : CommandBase
{
    public BossHpCommand() : base(0, "bosshp")
    {
        Description = "Show HP of bosses on current map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        foreach (var monster in player.getMap().getAllMonsters())
        {
            if (monster != null && monster.isBoss() && monster.getHp() > 0)
            {
                long percent = monster.getHp() * 100L / monster.getMaxHp();
                string bar = "[";
                for (int i = 0; i < 100; i++)
                {
                    bar += i < percent ? "|" : ".";
                }
                bar += "]";
                player.yellowMessage(monster.getName() + " (" + monster.getId() + ") has " + percent + "% HP left.");
                player.yellowMessage("HP: " + bar);
            }
        }
    }
}
