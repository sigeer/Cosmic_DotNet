namespace Application.Core.Game.Commands.Gm0;
public class MobHpCommand : CommandBase
{
    public MobHpCommand() : base(0, "mobhp")
    {
        Description = "Show HP of mobs on current map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        foreach (var monster in player.getMap().getAllMonsters())
        {
            if (monster != null && monster.getHp() > 0)
            {
                player.yellowMessage(monster.getName() + " (" + monster.getId() + ") has " + monster.getHp() + " / " + monster.getMaxHp() + " HP.");

            }
        }
    }
}
