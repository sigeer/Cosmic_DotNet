using net.server;
using server.expeditions;

namespace Application.Core.Game.Commands.Gm3;

public class ExpedsCommand : CommandBase
{
    public ExpedsCommand() : base(3, "expeds")
    {
        Description = "Show all ongoing boss expeditions.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        foreach (var ch in Server.getInstance().getChannelsFromWorld(c.getWorld()))
        {
            List<Expedition> expeds = ch.getExpeditions();
            if (expeds.Count == 0)
            {
                player.yellowMessage("No Expeditions in Channel " + ch.getId());
                continue;
            }
            player.yellowMessage("Expeditions in Channel " + ch.getId());
            int id = 0;
            foreach (Expedition exped in expeds)
            {
                id++;
                player.yellowMessage("> Expedition " + id);
                player.yellowMessage(">> Type: " + exped.getType().ToString());
                player.yellowMessage(">> Status: " + (exped.isRegistering() ? "REGISTERING" : "UNDERWAY"));
                player.yellowMessage(">> Size: " + exped.getMembers().Count);
                player.yellowMessage(">> Leader: " + exped.getLeader().getName());
                int memId = 2;
                foreach (var e in exped.getMembers())
                {
                    if (exped.isLeader(e.Key))
                    {
                        continue;
                    }
                    player.yellowMessage(">>> Member " + memId + ": " + e.Value);
                    memId++;
                }
            }
        }
    }
}
