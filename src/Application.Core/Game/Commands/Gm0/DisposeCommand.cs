using scripting.npc;
using scripting.quest;
using tools;

namespace Application.Core.Game.Commands.Gm0;

public class DisposeCommand : CommandBase
{
    public DisposeCommand(): base(0, "dispose")
    {

    }
    public override void Execute(IClient c, string[] paramValues)
    {
        NPCScriptManager.getInstance().dispose(c);
        QuestScriptManager.getInstance().dispose(c);
        c.sendPacket(PacketCreator.enableActions());
        c.removeClickedNPC();
        c.OnlinedCharacter.message("You've been disposed.");
    }
}
