using Application.Core.Compatible.Extensions;
using Application.Core.Game.Commands;

namespace ServiceTest.Games
{
    public class CommandTests : TestBase
    {
        [Test]
        public void GetGmCommandsTest()
        {
            var data = CommandExecutor.getInstance().getGmCommands();
            Assert.That(data.Size(), Is.EqualTo(7));
        }
    }
}
