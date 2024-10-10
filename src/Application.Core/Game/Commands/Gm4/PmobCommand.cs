using Application.Core.constants.game;
using server.life;

namespace Application.Core.Game.Commands.Gm4;

public class PmobCommand : CommandBase
{
    public PmobCommand() : base(4, "pmob")
    {
        Description = "Spawn a permanent mob on your location.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !pmob <mobid> [<mobtime>]");
            return;
        }

        // command suggestion thanks to HighKey21, none, bibiko94 (TAYAMO), asafgb
        int mapId = player.getMapId();
        int mobId = int.Parse(paramsValue[0]);
        int mobTime = (paramsValue.Length > 1) ? int.Parse(paramsValue[1]) : -1;

        Point checkpos = player.getMap().getGroundBelow(player.getPosition());
        int xpos = checkpos.X;
        int ypos = checkpos.Y;
        int fh = player.getMap().getFootholds().findBelow(checkpos).getId();

        var mob = LifeFactory.getMonster(mobId);
        if (mob != null && !mob.getName().Equals("MISSINGNO"))
        {
            mob.setPosition(checkpos);
            mob.setCy(ypos);
            mob.setRx0(xpos + 50);
            mob.setRx1(xpos - 50);
            mob.setFh(fh);
            try
            {

                using var dbContext = new DBContext();
                Plife newModel = new Plife(player.getWorld(), mapId, mobId, mobTime, xpos, ypos, fh, LifeType.Monster);
                dbContext.Plives.Add(newModel);
                dbContext.SaveChanges();

                foreach (var ch in player.getWorldServer().getChannels())
                {
                    var map = ch.getMapFactory().getMap(mapId);
                    map.addMonsterSpawn(mob, mobTime, -1);
                    map.addAllMonsterSpawn(mob, mobTime, -1);
                }

                player.yellowMessage("Pmob created.");
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                player.dropMessage(5, "Failed to store pmob in the database.");
            }
        }
        else
        {
            player.dropMessage(5, "You have entered an invalid mob id.");
        }
    }
}