using System.Text.Json;

namespace Application.Core.Game.QuestDomain.RewardAdapter
{
    public interface IRewardSkillAdapter : IRewardAdapter
    {
        Dictionary<int, SkillData> GetData();
    }
    public class EntityRewardSkillAdapter : IRewardSkillAdapter
    {
        readonly QuestRewardEntity _data;

        public EntityRewardSkillAdapter(QuestRewardEntity data)
        {
            _data = data;
        }

        public Dictionary<int, SkillData> GetData()
        {
            return JsonSerializer.Deserialize<Dictionary<int, SkillData>>(_data.Value ?? "[]") ?? [];
        }
    }
    public class WzRewardSkillAdapter : IRewardSkillAdapter
    {
        readonly Data _data;

        public WzRewardSkillAdapter(Data data)
        {
            _data = data;
        }

        public Dictionary<int, SkillData> GetData()
        {
            Dictionary<int, SkillData> skillData = new();
            foreach (Data sEntry in _data)
            {
                byte skillLevel = 0;
                int skillid = DataTool.getInt(sEntry.getChildByPath("id"));
                var skillLevelData = sEntry.getChildByPath("skillLevel");
                if (skillLevelData != null)
                {
                    skillLevel = (byte)DataTool.getInt(skillLevelData);
                }
                int masterLevel = DataTool.getInt(sEntry.getChildByPath("masterLevel"));
                List<int> jobs = new();

                var applicableJobs = sEntry.getChildByPath("job");
                if (applicableJobs != null)
                {
                    foreach (Data applicableJob in applicableJobs.getChildren())
                    {
                        jobs.Add(DataTool.getInt(applicableJob));
                    }
                }

                skillData[skillid] = new SkillData(skillid, skillLevel, masterLevel, jobs);
            }
            return skillData;
        }
    }
}
