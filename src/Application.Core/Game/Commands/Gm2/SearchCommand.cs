using constants.id;
using provider;
using provider.wz;
using server;
using server.quest;
using System.Text;

namespace Application.Core.Game.Commands.Gm2;

public class SearchCommand : CommandBase
{
    private Data npcStringData;
    private Data mobStringData;
    private Data skillStringData;
    private Data mapStringData;

    public SearchCommand() : base(2, "search")
    {
        Description = "Search string.wz.";

        DataProvider dataProvider = DataProviderFactory.getDataProvider(WZFiles.STRING);
        npcStringData = dataProvider.getData("Npc.img");
        mobStringData = dataProvider.getData("Mob.img");
        skillStringData = dataProvider.getData("Skill.img");
        mapStringData = dataProvider.getData("Map.img");
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 2)
        {
            player.yellowMessage("Syntax: !search <type> <name>");
            return;
        }
        StringBuilder sb = new StringBuilder();

        string search = joinStringFrom(paramsValue, 1);
        long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();//for the lulz
        Data? data = null;
        if (!paramsValue[0].Equals("ITEM", StringComparison.OrdinalIgnoreCase))
        {
            int searchType = 0;

            if (paramsValue[0].Equals("NPC", StringComparison.OrdinalIgnoreCase))
            {
                data = npcStringData;
            }
            else if (paramsValue[0].Equals("MOB", StringComparison.OrdinalIgnoreCase) || paramsValue[0].Equals("MONSTER", StringComparison.OrdinalIgnoreCase))
            {
                data = mobStringData;
            }
            else if (paramsValue[0].Equals("SKILL", StringComparison.OrdinalIgnoreCase))
            {
                data = skillStringData;
            }
            else if (paramsValue[0].Equals("MAP", StringComparison.OrdinalIgnoreCase))
            {
                data = mapStringData;
                searchType = 1;
            }
            else if (paramsValue[0].Equals("QUEST", StringComparison.OrdinalIgnoreCase))
            {
                data = mapStringData;
                searchType = 2;
            }
            else
            {
                sb.Append("#bInvalid search.\r\nSyntax: '!search [type] [name]', where [type] is MAP, QUEST, NPC, ITEM, MOB, or SKILL.");
            }
            if (data != null)
            {
                string name;

                if (searchType == 0)
                {
                    foreach (Data searchData in data.getChildren())
                    {
                        name = DataTool.getString(searchData.getChildByPath("name")) ?? "NO-NAME";
                        if (name.ToLower().Contains(search.ToLower()))
                        {
                            sb.Append("#b").Append(int.Parse(searchData.getName())).Append("#k - #r").Append(name).Append("\r\n");
                        }
                    }
                }
                else if (searchType == 1)
                {
                    string mapName, streetName;

                    foreach (Data searchDataDir in data.getChildren())
                    {
                        foreach (Data searchData in searchDataDir.getChildren())
                        {
                            mapName = DataTool.getString(searchData.getChildByPath("mapName")) ?? "NO-NAME";
                            streetName = DataTool.getString(searchData.getChildByPath("streetName")) ?? "NO-NAME";

                            if (mapName.ToLower().Contains(search.ToLower()) || streetName.ToLower().Contains(search.ToLower()))
                            {
                                sb.Append("#b").Append(int.Parse(searchData.getName())).Append("#k - #r").Append(streetName).Append(" - ").Append(mapName).Append("\r\n");
                            }
                        }
                    }
                }
                else
                {
                    foreach (Quest mq in Quest.getMatchedQuests(search))
                    {
                        sb.Append("#b").Append(mq.getId()).Append("#k - #r");

                        string parentName = mq.getParentName();
                        if (parentName.Count() > 0)
                        {
                            sb.Append(parentName).Append(" - ");
                        }
                        sb.Append(mq.getName()).Append("\r\n");
                    }
                }
            }
        }
        else
        {
            foreach (var itemPair in ItemInformationProvider.getInstance().getAllItems())
            {
                if (sb.Length < 32654)
                {//ohlol
                    if (itemPair.Name.ToLower().Contains(search.ToLower()))
                    {
                        sb.Append("#b").Append(itemPair.Id).Append("#k - #r").Append(itemPair.Name).Append("\r\n");
                    }
                }
                else
                {
                    sb.Append("#bCouldn't load all items, there are too many results.\r\n");
                    break;
                }
            }
        }
        if (sb.Length == 0)
        {
            sb.Append("#bNo ").Append(paramsValue[0].ToLower()).Append("s found.\r\n");
        }
        sb.Append("\r\n#kLoaded within ").Append((double)(DateTimeOffset.Now.ToUnixTimeMilliseconds() - start) / 1000).Append(" seconds.");//because I can, and it's free

        c.getAbstractPlayerInteraction().npcTalk(NpcId.MAPLE_ADMINISTRATOR, sb.ToString());
    }
}
