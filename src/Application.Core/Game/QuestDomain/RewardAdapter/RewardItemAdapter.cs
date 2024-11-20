using System.Text.Json;

namespace Application.Core.Game.QuestDomain.RewardAdapter
{
    public interface IRewardItemAdapter : IRewardAdapter
    {
        List<ItemData> GetData();
    }

    public class EntityRewardItemAdapter : IRewardItemAdapter
    {
        readonly QuestRewardEntity _data;

        public EntityRewardItemAdapter(QuestRewardEntity data)
        {
            _data = data;
        }

        public List<ItemData> GetData()
        {
            return JsonSerializer.Deserialize<List<ItemData>>(_data.Value ?? "[]") ?? [];
        }
    }

    public class WzRewardItemAdapter : IRewardItemAdapter
    {
        readonly Data _data;

        public WzRewardItemAdapter(Data data)
        {
            _data = data;
        }

        public List<ItemData> GetData()
        {
            List<ItemData> items = new();
            foreach (Data iEntry in _data.getChildren())
            {
                int id = DataTool.getInt(iEntry.getChildByPath("id"));
                int count = DataTool.getInt(iEntry.getChildByPath("count"), 1);
                int period = DataTool.getInt(iEntry.getChildByPath("period"), 0);

                int? prop = null;
                var propData = iEntry.getChildByPath("prop");
                if (propData != null)
                {
                    prop = DataTool.getInt(propData);
                }

                int gender = 2;
                if (iEntry.getChildByPath("gender") != null)
                {
                    gender = DataTool.getInt(iEntry.getChildByPath("gender"));
                }

                int job = -1;
                if (iEntry.getChildByPath("job") != null)
                {
                    job = DataTool.getInt(iEntry.getChildByPath("job"));
                }

                items.Add(new ItemData(int.Parse(iEntry.getName()), id, count, prop, job, gender, period));
            }

            items.Sort((o1, o2) => o1.map - o2.map);
            return items;
        }
    }
}
