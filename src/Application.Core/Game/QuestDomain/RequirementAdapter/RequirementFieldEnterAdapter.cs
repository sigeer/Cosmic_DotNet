namespace Application.Core.Game.QuestDomain.RequirementAdapter
{
    public interface IRequirementFieldEnterAdapter : IRequirementAdapter
    {
        int GetMapId();
    }

    public class EntityRequirementFieldEnterAdapter : IRequirementFieldEnterAdapter
    {
        readonly QuestRequirementEntity _data;

        public EntityRequirementFieldEnterAdapter(QuestRequirementEntity data)
        {
            _data = data;
        }

        public int GetMapId()
        {
            return int.TryParse(_data.Value, out var d) ? d : -1;
        }
    }

    public class WzRequirementFieldEnterAdapter : IRequirementFieldEnterAdapter
    {
        readonly Data _data;

        public WzRequirementFieldEnterAdapter(Data data)
        {
            _data = data;
        }

        public int GetMapId()
        {
            var zeroField = _data.getChildByPath("0");
            if (zeroField != null)
            {
                return DataTool.getInt(zeroField);
            }
            return -1;
        }
    }
}
