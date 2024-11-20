using System.Text.Json;

namespace Application.Core.Game.QuestDomain.RequirementAdapter
{
    public interface IRequirementPetAdapter : IRequirementAdapter
    {
        List<int> GetData();
    }


    public class EntityRequirementPetAdapter : IRequirementPetAdapter
    {
        readonly QuestRequirementEntity _data;

        public EntityRequirementPetAdapter(QuestRequirementEntity data)
        {
            _data = data;
        }

        public List<int> GetData()
        {
            return JsonSerializer.Deserialize<List<int>>(_data.Value ?? "[]") ?? [];
        }
    }

    public class WzRequirementPetAdapter : IRequirementPetAdapter
    {
        readonly Data _data;

        public WzRequirementPetAdapter(Data data)
        {
            _data = data;
        }

        public List<int> GetData()
        {
            return _data.getChildren().Select(x => DataTool.getInt(x.getChildByPath("id"))).ToList();
        }
    }
}
