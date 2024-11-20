using System.Text.Json;

namespace Application.Core.Game.QuestDomain.RewardAdapter
{

    public interface IRewardQuestAdapter : IRewardAdapter
    {
        Dictionary<int, int> GetQuests();
    }
    public class EntityRewardQuestAdapter : IRewardQuestAdapter
    {
        readonly QuestRewardEntity _data;

        public EntityRewardQuestAdapter(QuestRewardEntity data)
        {
            _data = data;
        }

        public Dictionary<int, int> GetQuests()
        {
            return JsonSerializer.Deserialize<Dictionary<int, int>>(_data.Value ?? "[]") ?? [];
        }
    }

    public class WzRewardQuestAdapter : IRewardQuestAdapter
    {
        readonly Data _data;

        public WzRewardQuestAdapter(Data data)
        {
            _data = data;
        }

        public Dictionary<int, int> GetQuests()
        {
            Dictionary<int, int> data = [];
            foreach (Data qEntry in _data)
            {
                int questid = DataTool.getInt(qEntry.getChildByPath("id"));
                int stat = DataTool.getInt(qEntry.getChildByPath("state"));
                data[questid] = stat;
            }
            return data;
        }
    }
}
