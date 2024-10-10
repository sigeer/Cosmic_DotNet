using constants.game;
using constants.id;
using server;
using System.Collections.Concurrent;
using tools.exceptions;

namespace Application.Core.Game.Commands.Gm2;


public class IdCommand : CommandBase
{
    public IdCommand() : base(2, "id")
    {
        Description = "Search in handbook.";

        handbookDirectory.Add("map", "handbook/Map.txt");
        handbookDirectory.Add("etc", "handbook/Etc.txt");
        handbookDirectory.Add("npc", "handbook/NPC.txt");
        handbookDirectory.Add("use", "handbook/Use.txt");
        handbookDirectory.Add("weapon", "handbook/Equip/Weapon.txt"); // TODO add more into this
    }
    private static int MAX_SEARCH_HITS = 100;
    private Dictionary<string, string> handbookDirectory = new();
    private ConcurrentDictionary<string, HandbookFileItems> typeItems = new();


    private class HandbookFileItems
    {
        private List<HandbookItem> items;

        public HandbookFileItems(List<string> fileLines)
        {
            this.items = fileLines.Select(x => parseLine(x)).Where(x => x != null).ToList();
        }

        private HandbookItem? parseLine(string line)
        {
            if (line == null)
            {
                return null;
            }

            string[] SplitLine = line.Split(" - ", 2);
            if (SplitLine.Length < 2 || string.IsNullOrEmpty(SplitLine[1]))
            {
                return null;
            }
            return new HandbookItem(SplitLine[0], SplitLine[1]);
        }

        public List<HandbookItem> search(string query)
        {
            if (query == null || string.IsNullOrEmpty(query))
            {
                return new List<HandbookItem>();
            }
            return items
                    .Where(item => item.matches(query))
                    .ToList();
        }
    }

    private record HandbookItem(string id, string name)
    {


        public bool matches(string query)
        {
            if (query == null)
            {
                return false;
            }
            return this.name.ToLower().Contains(query.ToLower());
        }
    }

    public override void Execute(IClient client, string[] paramsValue)
    {
        var chr = client.OnlinedCharacter;
        if (paramsValue.Length < 2)
        {
            chr.yellowMessage("Syntax: !id <type> <query>");
            return;
        }
        string type = paramsValue[0].ToLower();
        string[] queryItems = Arrays.copyOfRange(paramsValue, 1, paramsValue.Length);
        string query = string.Join(" ", queryItems);
        chr.yellowMessage("Querying for entry... May take some time... Please try to refine your search.");
        var queryRunnable = () =>
        {
            try
            {
                populateIdMap(type);

                var handbookFileItems = typeItems.GetValueOrDefault(type);
                if (handbookFileItems == null)
                {
                    return;
                }
                List<HandbookItem> searchHits = handbookFileItems.search(query);

                if (searchHits.Count > 0)
                {
                    string searchHitsText = string.Join(NpcChat.NEW_LINE, searchHits
                            .Take(MAX_SEARCH_HITS)
                            .Select(x => string.Format("Id for {0} is: #b{1}#k", x.name, x.id)));
                    int hitsCount = Math.Min(searchHits.Count, MAX_SEARCH_HITS);
                    string summaryText = string.Format("Results found: #r{0}#k | Returned: #b{1}#k/100 | Refine search query to improve time.", searchHits.Count, hitsCount);
                    string fullText = searchHitsText + NpcChat.NEW_LINE + summaryText;
                    chr.getAbstractPlayerInteraction().npcTalk(NpcId.MAPLE_ADMINISTRATOR, fullText);
                }
                else
                {
                    chr.yellowMessage(string.Format("Id not found for item: {0}, of type: {1}.", query, type));
                }
            }
            catch (IdTypeNotSupportedException e)
            {
                log.Error(e.ToString());
                chr.yellowMessage("Your query type is not supported.");
            }
            catch (IOException e)
            {
                log.Error(e.ToString());
                chr.yellowMessage("Error reading file, please contact your administrator.");
            }
        };

        ThreadManager.getInstance().newTask(queryRunnable);
    }

    private void populateIdMap(string type)
    {
        var filePath = handbookDirectory.GetValueOrDefault(type);
        if (filePath == null)
        {
            throw new IdTypeNotSupportedException();
        }
        if (typeItems.ContainsKey(type))
        {
            return;
        }

        var fileLines = File.ReadAllLines(filePath).ToList();
        typeItems.AddOrUpdate(type, new HandbookFileItems(fileLines));
    }
}
