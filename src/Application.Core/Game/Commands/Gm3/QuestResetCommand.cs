using server.quest;

namespace Application.Core.Game.Commands.Gm3;

public class QuestResetCommand : CommandBase
{
    public QuestResetCommand() : base(3, "resetquest")
    {
        Description = "Reset a completed quest.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !resetquest <questid>");
            return;
        }

        int questid_ = int.Parse(paramsValue[0]);

        if (player.getQuestStatus(questid_) != 0)
        {
            Quest quest = Quest.getInstance(questid_);
            if (quest != null)
            {
                quest.reset(player);
                player.dropMessage(5, "QUEST " + questid_ + " reseted.");
            }
            else
            {    // should not occur
                player.dropMessage(5, "QUESTID " + questid_ + " is invalid.");
            }
        }
    }
}
