using Application.Core.Game;
using Application.Core.Game.GameEvents.PartyQuest;
using Application.Core.Managers;

namespace ServiceTest.Games
{
    public class PQTests : TestBase
    {
        [Test]
        public void KerningPQTest()
        {
            MockClient.getChannelServer().reloadEventScriptManager();

            var pq = new KerningPQ(MockClient.OnlinedCharacter);
            MockClient.OnlinedCharacter.Level = 10;
            pq.MinLevel = 1;
            pq.MinCount = 1;
            TeamManager.createParty(MockClient.OnlinedCharacter, true);
            pq.StartQuest();
            Assert.Pass();
        }
    }
}
