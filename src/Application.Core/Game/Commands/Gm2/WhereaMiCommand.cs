using Application.Core.Game.Life;

namespace Application.Core.Game.Commands.Gm2;

public class WhereaMiCommand : CommandBase
{
    public WhereaMiCommand() : base(2, "whereami")
    {
        Description = "Show info about objects on current map.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        HashSet<IPlayer> chars = new();
        HashSet<NPC> npcs = new();
        HashSet<PlayerNPC> playernpcs = new();
        HashSet<Monster> mobs = new();

        foreach (var mmo in player.getMap().getMapObjects())
        {
            if (mmo is NPC npc)
            {
                npcs.Add(npc);
            }
            else if (mmo is IPlayer mc)
            {
                chars.Add(mc);
            }
            else if (mmo is Monster mob)
            {
                if (mob.isAlive())
                {
                    mobs.Add(mob);
                }
            }
            else if (mmo is PlayerNPC pnpc)
            {
                playernpcs.Add(pnpc);
            }
        }

        player.yellowMessage("Map ID: " + player.getMap().getId());

        player.yellowMessage("Players on this map:");
        foreach (var chr in chars)
        {
            player.dropMessage(5, ">> " + chr.getName() + " - " + chr.getId() + " - Oid: " + chr.getObjectId());
        }

        if (playernpcs.Count > 0)
        {
            player.yellowMessage("PlayerNPCs on this map:");
            foreach (PlayerNPC pnpc in playernpcs)
            {
                player.dropMessage(5, ">> " + pnpc.getName() + " - Scriptid: " + pnpc.getScriptId() + " - Oid: " + pnpc.getObjectId());
            }
        }

        if (npcs.Count > 0)
        {
            player.yellowMessage("NPCs on this map:");
            foreach (NPC npc in npcs)
            {
                player.dropMessage(5, ">> " + npc.getName() + " - " + npc.getId() + " - Oid: " + npc.getObjectId());
            }
        }

        if (mobs.Count > 0)
        {
            player.yellowMessage("Monsters on this map:");
            foreach (Monster mob in mobs)
            {
                if (mob.isAlive())
                {
                    player.dropMessage(5, ">> " + mob.getName() + " - " + mob.getId() + " - Oid: " + mob.getObjectId());
                }
            }
        }
    }
}
