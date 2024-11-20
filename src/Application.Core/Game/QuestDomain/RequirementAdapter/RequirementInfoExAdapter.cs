using System.Text.Json;

namespace Application.Core.Game.QuestDomain.RequirementAdapter
{
    public interface IRequirementInfoExAdapter : IRequirementAdapter
    {
        List<string> GetData();
    }

    public class EntityRequirementInfoExAdapter: IRequirementInfoExAdapter
    {
        readonly QuestRequirementEntity _data;

        public EntityRequirementInfoExAdapter(QuestRequirementEntity data)
        {
            _data = data;
        }

        public List<string> GetData()
        {
            return JsonSerializer.Deserialize<List<string>>(_data.Value ?? "[]") ?? [];
        }
    }

    public class WzRequirementInfoExAdapter : IRequirementInfoExAdapter
    {
        readonly Data _data;

        public WzRequirementInfoExAdapter(Data data)
        {
            _data = data;
        }

        public List<string> GetData()
        {
            return _data.getChildren().Select(infoEx => DataTool.getString(infoEx.getChildByPath("value")) ?? "").ToList();
        }
    }
}
