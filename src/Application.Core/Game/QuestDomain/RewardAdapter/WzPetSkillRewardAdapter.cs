namespace Application.Core.Game.QuestDomain.RewardAdapter
{

    public class WzPetSkillRewardAdapter : WzRewardDataAdapter
    {
        public WzPetSkillRewardAdapter(Data data) : base(data)
        {
        }

        public override int GetIntValue()
        {
            return DataTool.getInt("petskill", _data);
        }
    }
}
