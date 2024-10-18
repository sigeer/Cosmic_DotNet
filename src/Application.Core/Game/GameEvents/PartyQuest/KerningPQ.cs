namespace Application.Core.Game.GameEvents.PartyQuest
{
    public class KerningPQ : PlayerPartyQuestBase
    {
        public KerningPQ(IPlayer player) : base("KerningPQ", "kpq", player)
        {
            EntryNpcId = 9020000;
        }

        public override int GetStageFromMap(int mapId) => mapId - FirstMapId + 1;
        public override int ClearMapId => 103000804;
    }
}
