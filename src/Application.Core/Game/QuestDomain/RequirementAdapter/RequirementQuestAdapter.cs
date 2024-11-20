using System.Text.Json;

namespace Application.Core.Game.QuestDomain.RequirementAdapter
{
    public interface IRequirementQuestAdapter : IRequirementAdapter
    {
        Dictionary<int, int> GetData();
    }

    public class EntityRequirementQuestAdapter : IRequirementQuestAdapter
    {
        readonly QuestRequirementEntity _data;

        public EntityRequirementQuestAdapter(QuestRequirementEntity data)
        {
            _data = data;
        }

        public Dictionary<int, int> GetData()
        {
            return JsonSerializer.Deserialize<Dictionary<int, int>>(_data.Value ?? "[]") ?? [];
        }
    }

    public class WzRequirementQuestAdapter : IRequirementQuestAdapter
    {
        readonly Data _data;

        public WzRequirementQuestAdapter(Data data)
        {
            _data = data;
        }

        public Dictionary<int, int> GetData()
        {
            Dictionary<int, int> quests = new();
            foreach (Data questEntry in _data.getChildren())
            {
                int questID = DataTool.getInt(questEntry.getChildByPath("id"));
                int stateReq = DataTool.getInt(questEntry.getChildByPath("state"));
                quests.AddOrUpdate(questID, stateReq);
            }
            return quests;
        }
    }
}
