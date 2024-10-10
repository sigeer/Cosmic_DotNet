using Application.Core.Managers;

namespace Application.Core.Game.Commands.Gm4;

public class PnpcCommand : CommandBase
{
    public PnpcCommand() : base(4, "pnpc")
    {
        Description = "Spawn a permanent NPC on your location.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !pnpc <npcid>");
            return;
        }

        if (!int.TryParse(paramsValue[0], out var npcId))
        {
            player.yellowMessage("Syntax: npcid invalid");
            return;
        }

        // command suggestion thanks to HighKey21, none, bibiko94 (TAYAMO), asafgb
        if (player.getMap().containsNPC(npcId))
        {
            player.dropMessage(5, "This map already contains the specified NPC.");
            return;
        }

        try
        {
            if (PLifeManager.CreatePnpc(npcId, player))
                player.yellowMessage("Pnpc created.");
            else
                player.dropMessage(5, "You have entered an invalid NPC id.");
        }
        catch (Exception ex)
        {
            log.Error(ex.ToString());
            player.dropMessage(5, "Failed to store pNPC in the database.");
        }
    }
}