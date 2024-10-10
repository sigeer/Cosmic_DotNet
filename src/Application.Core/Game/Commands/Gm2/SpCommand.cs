namespace Application.Core.Game.Commands.Gm2;

public class SpCommand : CommandBase
{
    public SpCommand() : base(2, "sp")
    {
        Description = "Set available SP.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !sp [<playername>] <newsp>");
            return;
        }

        if (paramsValue.Length == 1)
        {
            int newSp = int.Parse(paramsValue[0]);
            if (newSp < 0)
            {
                newSp = 0;
            }
            else if (newSp > YamlConfig.config.server.MAX_AP)
            {
                newSp = YamlConfig.config.server.MAX_AP;
            }

            player.updateRemainingSp(newSp);
        }
        else
        {
            var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
            if (victim != null && victim.IsOnlined)
            {
                int newSp = int.Parse(paramsValue[1]);
                if (newSp < 0)
                {
                    newSp = 0;
                }
                else if (newSp > YamlConfig.config.server.MAX_AP)
                {
                    newSp = YamlConfig.config.server.MAX_AP;
                }

                victim.updateRemainingSp(newSp);

                player.dropMessage(5, "SP given.");
            }
            else
            {
                player.message("Player '" + paramsValue[0] + "' could not be found.");
            }
        }
    }
}
