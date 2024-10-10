using constants.id;
using server;
using server.life;

namespace Application.Core.Game.Commands.Gm1;
public class WhatDropsFromCommand : CommandBase
{
    public WhatDropsFromCommand() : base(1, "whatdropsfrom")
    {
        Description = "Show what items drop from a mob.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.dropMessage(5, "Please do @whatdropsfrom <monster name>");
            return;
        }
        string monsterName = player.getLastCommandMessage();
        string output = "";
        int limit = 3;
        var dataList = MonsterInformationProvider.getMobsIDsFromName(monsterName).Take(limit);
        foreach (var data in dataList)
        {

            int mobId = data.Key;
            string mobName = data.Value;
            output += mobName + " drops the following items:\r\n\r\n";
            foreach (var drop in MonsterInformationProvider.getInstance().retrieveDrop(mobId))
            {
                try
                {
                    var name = ItemInformationProvider.getInstance().getName(drop.ItemId);
                    if (name == null || name.Equals("null") || drop.Chance == 0)
                    {
                        continue;
                    }
                    float chance = Math.Max(1000000 / drop.Chance / (!MonsterInformationProvider.getInstance().isBoss(mobId) ? player.getDropRate() : player.getBossDropRate()), 1);
                    output += "- " + name + " (1/" + (int)chance + ")\r\n";
                }
                catch (Exception ex)
                {
                    log.Error(ex.ToString());
                    continue;
                }
            }
            output += "\r\n";

        }

        c.getAbstractPlayerInteraction().npcTalk(NpcId.MAPLE_ADMINISTRATOR, output);
    }
}
