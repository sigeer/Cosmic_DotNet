using server.quest;

namespace Application.Core.Game.Commands.Gm3;

public class QuestCompleteCommand : CommandBase
{
    public QuestCompleteCommand() : base(3, "completequest")
    {
        Description = "Complete an active quest.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !completequest <questid>");
            return;
        }

        int questId = int.Parse(paramsValue[0]);

        if (player.getQuestStatus(questId) == 1)
        {
            Quest quest = Quest.getInstance(questId);
            if (quest != null && quest.getNpcRequirement(true) != -1)
            {
                c.getAbstractPlayerInteraction().forceCompleteQuest(questId, quest.getNpcRequirement(true));
            }
            else
            {
                c.getAbstractPlayerInteraction().forceCompleteQuest(questId);
            }

            player.dropMessage(5, "QUEST " + questId + " completed.");
        }
        else
        {
            player.dropMessage(5, "QUESTID " + questId + " not started or already completed.");
        }
    }
}
