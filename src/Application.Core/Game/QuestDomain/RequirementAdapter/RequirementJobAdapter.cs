using System.Text.Json;

namespace Application.Core.Game.QuestDomain.RequirementAdapter
{
    public interface IRequirementJobAdapter: IRequirementAdapter
    {
        List<int> GetData();
    }


    public class EntityRequirementJobAdapter : IRequirementJobAdapter
    {
        readonly QuestRequirementEntity _data;

        public EntityRequirementJobAdapter(QuestRequirementEntity data)
        {
            _data = data;
        }

        public List<int> GetData()
        {
            return JsonSerializer.Deserialize<List<int>>(_data.Value ?? "[]") ?? [];
        }
    }

    public class WzRequirementJobAdapter : IRequirementJobAdapter
    {
        readonly Data _data;

        public WzRequirementJobAdapter(Data data)
        {
            _data = data;
        }

        public List<int> GetData()
        {
            return _data.getChildren().Select(x => DataTool.getInt(x)).ToList();
        }
    }
}
