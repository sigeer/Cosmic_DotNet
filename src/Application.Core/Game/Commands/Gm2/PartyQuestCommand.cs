using Application.Core.Game.GameEvents.PartyQuest;

namespace Application.Core.Game.Commands.Gm2
{
    public class PartyQuestCommand : ParamsCommandBase
    {
        public PartyQuestCommand() : base(["[kpq|lpq|ellinpq]", "[start|complete]"], 2, "fb")
        {
            Description = "强制开启团队任务，完成当前关卡。";
        }

        public override void Execute(IClient client, string[] values)
        {
            PlayerPartyQuestBase? pq = null;
            if (values[0] == "kpq")
            {
                pq = new KerningPQ(client.OnlinedCharacter);
            }

            if (values[0] == "lpq")
            {
                pq = new LudiPQ(client.OnlinedCharacter);
            }

            if (values[0] == "ellinpq")
            {
                pq = new EllinPQ(client.OnlinedCharacter);
            }

            if (pq == null)
            {
                client.OnlinedCharacter.yellowMessage($"暂不支持<{values[0]}>对应的团队任务。");
                return;
            }

            if (values[1] == "start")
            {
                pq.MinCount = 1;
                pq.MinLevel = 1;
                pq.StartQuest();
            }
            else
            {
                pq.CompleteStage();
            }

        }

    }
}
