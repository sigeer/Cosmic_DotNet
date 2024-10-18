namespace Application.Core.Game.GameEvents.PartyQuest
{
    public class LudiPQ : PlayerPartyQuestBase
    {
        public LudiPQ(IPlayer player) : base("LudiPQ", "lpq", player)
        {
            EntryNpcId = 2040034;
        }

        public override int GetStageFromMap(int mapId) => ((mapId - 922010100) / 100) + 1;
        public override int ClearMapId => 922010900;
    }
}
