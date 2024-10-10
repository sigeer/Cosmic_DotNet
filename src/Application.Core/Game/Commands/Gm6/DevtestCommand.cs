using Application.Core.Scripting.Infrastructure;
using scripting;

namespace Application.Core.Game.Commands.Gm6;

public class DevtestCommand : CommandBase
{
    public DevtestCommand() : base(6, "devtest")
    {
        Description = "Runs devtest.js. Developer utility - test stuff without restarting the server.";
    }

    private class DevtestScriptManager : AbstractScriptManager
    {

        public IEngine? GetScriptEngine(string path)
        {
            return base.getInvocableScriptEngine(path);
        }

    }

    public override void Execute(IClient client, string[] paramsValue)
    {
        DevtestScriptManager scriptManager = new DevtestScriptManager();
        var scriptEngine = scriptManager.GetScriptEngine("devtest.js");
        try
        {
            scriptEngine?.CallFunction("run", client.getPlayer());
        }
        catch (Exception e)
        {
            log.Information(e, "devtest.js run() threw an exception");
        }
    }
}
