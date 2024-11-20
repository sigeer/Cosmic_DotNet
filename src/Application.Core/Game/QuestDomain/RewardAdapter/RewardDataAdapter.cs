namespace Application.Core.Game.QuestDomain.RewardAdapter
{
    public interface IRewardDataAdapter : IRewardAdapter
    {
        public int GetIntValue();
        public string GetStringValue();
    }

    public class EntityRewardDataAdapter : IRewardDataAdapter
    {
        readonly QuestRewardEntity _data;

        public EntityRewardDataAdapter(QuestRewardEntity data)
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

    public class WzRewardDataAdapter : IRewardDataAdapter
    {
        protected readonly Data _data;

        public WzRewardDataAdapter(Data data)
        {
            _data = data;
        }

        public virtual int GetIntValue()
        {
            return DataTool.getInt(_data);
        }

        public string GetStringValue()
        {
            return DataTool.getString(_data);
        }
    }
}
