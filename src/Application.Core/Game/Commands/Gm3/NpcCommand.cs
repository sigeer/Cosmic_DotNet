using server.life;
using tools;

namespace Application.Core.Game.Commands.Gm3;

public class NpcCommand : CommandBase
{
    public NpcCommand() : base(3, "npc")
    {
        Description = "Spawn an NPC on your location.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !npc <npcid>");
            return;
        }
        var npc = LifeFactory.getNPC(int.Parse(paramsValue[0]));
        if (npc != null)
        {
            npc.setPosition(player.getPosition());
            npc.setCy(player.getPosition().Y);
            npc.setRx0(player.getPosition().X + 50);
            npc.setRx1(player.getPosition().X - 50);
            npc.setFh(player.getMap().getFootholds().findBelow(c.OnlinedCharacter.getPosition()).getId());
            player.getMap().addMapObject(npc);
            player.getMap().broadcastMessage(PacketCreator.spawnNPC(npc));
        }
    }
}
