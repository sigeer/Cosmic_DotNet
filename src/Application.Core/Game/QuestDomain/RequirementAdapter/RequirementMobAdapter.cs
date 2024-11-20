using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XmlWzReader;
using ZstdSharp.Unsafe;

namespace Application.Core.Game.QuestDomain.RequirementAdapter
{
    public interface IRequirementMobAdapter: IRequirementAdapter
    {
        Dictionary<int, int> GetData();
    }

    public class EntityRequirementMobAdapter : IRequirementMobAdapter
    {
        readonly QuestRequirementEntity _data;

        public EntityRequirementMobAdapter(QuestRequirementEntity data)
        {
            _data = data;
        }

        public Dictionary<int, int> GetData()
        {
            return JsonSerializer.Deserialize<Dictionary<int, int>>(_data.Value ?? "[]") ?? [];
        }
    }

    public class WzRequirementMobAdapter: IRequirementMobAdapter
    {
        readonly Data _data;

        public WzRequirementMobAdapter(Data data)
        {
            _data = data;
        }

        public Dictionary<int, int> GetData()
        {
            Dictionary<int, int> mobs = [];
            foreach (Data questEntry in _data.getChildren())
            {
                int mobID = DataTool.getInt(questEntry.getChildByPath("id"));
                int countReq = DataTool.getInt(questEntry.getChildByPath("count"));
                mobs.AddOrUpdate(mobID, countReq);
            }
            return mobs;
        }
    }
}
