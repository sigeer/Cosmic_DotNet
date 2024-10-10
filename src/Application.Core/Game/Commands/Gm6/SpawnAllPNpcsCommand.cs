using Application.Core.Game.Life;

namespace Application.Core.Game.Commands.Gm6;

public class SpawnAllPNpcsCommand : CommandBase
{
    public SpawnAllPNpcsCommand() : base(6, "spawnallpnpcs")
    {
        Description = "Spawn player NPCs of all existing players.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        PlayerNPC.multicastSpawnPlayerNPC(player.getMapId(), player.getWorld());
    }
}
