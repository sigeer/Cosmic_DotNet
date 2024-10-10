using server.quest;

namespace Application.Core.Game.Commands.Gm3;

public class QuestStartCommand : CommandBase
{
    public QuestStartCommand() : base(3, "startquest")
    {
        Description = "Start a quest.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !startquest <questid>");
            return;
        }

        int questid = int.Parse(paramsValue[0]);

        if (player.getQuestStatus(questid) == 0)
        {
            Quest quest = Quest.getInstance(questid);
            if (quest != null && quest.getNpcRequirement(false) != -1)
            {
                c.getAbstractPlayerInteraction().forceStartQuest(questid, quest.getNpcRequirement(false));
            }
            else
            {
                c.getAbstractPlayerInteraction().forceStartQuest(questid);
            }

            player.dropMessage(5, "QUEST " + questid + " started.");
        }
        else
        {
            player.dropMessage(5, "QUESTID " + questid + " already started/completed.");
        }
    }
}
