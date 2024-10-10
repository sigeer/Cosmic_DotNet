using scripting.npc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceTest.Scripts
{
    public class NPCScriptTests: TestBase
    {
        [Test]
        public void Script_2100_Test()
        {
            NPCScriptManager.getInstance().start(GetOnlinedTestClient(), 2100, null);
            Assert.Pass();
        }

        [Test]
        public void Script_CommandJs_Test()
        {
            var client = GetOnlinedTestClient();
            NPCScriptManager.getInstance().start(client, 0, "commands", null);
            NPCScriptManager.getInstance().action(client, 1, 1, 2);
            Assert.Pass();
        }
    }
}
