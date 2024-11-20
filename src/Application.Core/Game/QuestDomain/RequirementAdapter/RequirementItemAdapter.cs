using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XmlWzReader;

namespace Application.Core.Game.QuestDomain.RequirementAdapter
{
    public interface IRequirementItemAdapter: IRequirementAdapter
    {
        Dictionary<int, int> GetData();
    }

    public class EntityRequirementItemAdapter: IRequirementItemAdapter
    {
        readonly QuestRequirementEntity _data;

        public EntityRequirementItemAdapter(QuestRequirementEntity data)
        {
            _data = data;
        }

        public Dictionary<int, int> GetData()
        {
            return JsonSerializer.Deserialize<Dictionary<int, int>>(_data.Value ?? "[]") ?? [];
        }
    }

    public class WzRequirementItemAdapter: IRequirementItemAdapter
    {
        readonly Data _data;

        public WzRequirementItemAdapter(Data data)
        {
            _data = data;
        }

        public Dictionary<int, int> GetData()
        {
            var dic = new Dictionary<int, int>();
            foreach (Data itemEntry in _data.getChildren())
            {
                int itemId = DataTool.getInt(itemEntry.getChildByPath("id"));
                int count = DataTool.getInt(itemEntry.getChildByPath("count"), 0);

                dic.AddOrUpdate(itemId, count);
            }
            return dic;
        }
    }
}
