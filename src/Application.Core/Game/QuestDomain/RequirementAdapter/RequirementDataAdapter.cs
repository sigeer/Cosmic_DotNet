namespace Application.Core.Game.QuestDomain.RequirementAdapter
{
    public interface IRequirementDataAdapter : IRequirementAdapter
    {
        int GetIntValue();
        string GetStringValue();
    }

    public class EntityRequirementDataAdapter : IRequirementDataAdapter
    {
        readonly QuestRequirementEntity _data;

        public EntityRequirementDataAdapter(QuestRequirementEntity data)
        {
            _data = data;
        }

        public int GetIntValue()
        {
            if (int.TryParse(_data.Value, out var d))
                return d;
            return 0;
        }

        public string GetStringValue()
        {
            return _data.Value;
        }
    }

    public class WzRequirementDataAdapter : IRequirementDataAdapter
    {
        readonly Data _data;

        public WzRequirementDataAdapter(Data data)
        {
            _data = data;
        }

        public int GetIntValue()
        {
            return DataTool.getInt(_data);
        }

        public string GetStringValue()
        {
            return DataTool.getString(_data);
        }
    }
}
