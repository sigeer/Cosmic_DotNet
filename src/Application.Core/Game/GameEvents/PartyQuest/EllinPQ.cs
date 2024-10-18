using scripting.Event;

namespace Application.Core.Game.GameEvents.PartyQuest
{
    public class EllinPQ : PlayerPartyQuestBase
    {
        public EllinPQ(IPlayer player) : base("EllinPQ", "", player)
        {
            EntryNpcId = 2133000;
        }

        public override int GetStageFromMap(int mapId) => (mapId % 1000) / 100;
        public override int ClearMapId => 930000700;

        protected override void PassStage(EventInstanceManager eim, int curStg, int curMapId)
        {
            eim.showClearEffect(curMapId);
        }
    }
}
