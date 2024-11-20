namespace Application.Core.Game.QuestDomain
{
    /// <summary>
    /// QuestInfo
    /// </summary>
    public class QuestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string ParentName { get; set; } = null!;
        public int TimeLimit { get; set; }
        public int TimeLimit2 { get; set; }
        public bool AutoStart { get; set; }
        public bool AutoPreComplete { get; set; }
        public bool AutoComplete { get; set; }
        public int MedalId { get; set; }
        public string? Type { get; set; }
    }
    /// <summary>
    /// Check
    /// </summary>
    public class QuestRequirementEntity
    {
        public int Id { get; set; }
        /// <summary>
        /// 0. 接取 1. 完成
        /// </summary>
        public int Step { get; set; }
        public int QuestId { get; set; }
        public string RequirementType { get; set; } = null!;
        public string? Value { get; set; }
    }
    /// <summary>
    /// Act
    /// </summary>
    public class QuestRewardEntity
    {
        public int Id { get; set; }
        /// <summary>
        /// 0. 接取 1. 完成
        /// </summary>
        public int Step { get; set; }
        public int QuestId { get; set; }

        public string RewardType { get; set; } = null!;
        public string? Value { get; set; }
    }
}
