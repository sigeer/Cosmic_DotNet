namespace Application.Core.Game.Commands.Gm2;

public class ApCommand : CommandBase
{
    public ApCommand() : base(2, "ap")
    {
        Description = "Set available AP.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !ap [<playername>] <newap>");
            return;
        }

        if (paramsValue.Length < 2)
        {
            int newAp = int.Parse(paramsValue[0]);
            if (newAp < 0)
            {
                newAp = 0;
            }
            else if (newAp > YamlConfig.config.server.MAX_AP)
            {
                newAp = YamlConfig.config.server.MAX_AP;
            }

            player.changeRemainingAp(newAp, false);
        }
        else
        {
            var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
            if (victim != null && victim.IsOnlined)
            {
                int newAp = int.Parse(paramsValue[1]);
                if (newAp < 0)
                {
                    newAp = 0;
                }
                else if (newAp > YamlConfig.config.server.MAX_AP)
                {
                    newAp = YamlConfig.config.server.MAX_AP;
                }

                victim.changeRemainingAp(newAp, false);
            }
            else
            {
                player.message("Player '" + paramsValue[0] + "' could not be found.");
            }
        }
    }
}
