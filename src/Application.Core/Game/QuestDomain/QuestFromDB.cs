using Microsoft.EntityFrameworkCore;
using server.quest;

namespace Application.Core.Game.QuestDomain
{
    public class QuestFromDB
    {
        public static Quest? LoadQuestFromDB(int questId)
        {
            var dbContext = new DBContext();
            var questInfo = dbContext.Quests.FirstOrDefault(x => x.Id == questId);
            if (questInfo == null)
                return null;

            var requirements = dbContext.QuestRequirements.AsNoTracking().Where(x => x.QuestId == questId).ToList();
            var rewards = dbContext.QuestRewards.AsNoTracking().Where(x => x.QuestId == questId).ToList();
            return new Quest(questInfo, requirements, rewards);
        }

        public static List<Quest> LoadQuestFromDB()
        {
            var dbContext = new DBContext();
            var questInfo = dbContext.Quests.AsNoTracking().ToList();

            var questIdList = questInfo.Select(x => x.Id).ToArray();
            var requirements = dbContext.QuestRequirements.AsNoTracking().Where(x => questIdList.Contains(x.QuestId)).ToList();
            var rewards = dbContext.QuestRewards.AsNoTracking().Where(x => questIdList.Contains(x.QuestId)).ToList();

            return questInfo.Select(x => new Quest(x, requirements.Where(y => y.QuestId == x.Id).ToList(), rewards.Where(y => y.QuestId == x.Id).ToList())).ToList();
        }
    }
}
