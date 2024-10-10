namespace Application.Core.Game.Commands.Gm6;

public class GetAccCommand : CommandBase
{
    public GetAccCommand() : base(6, "getacc")
    {
        Description = "Show account name of an online player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !getacc <playername>");
            return;
        }
        var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(paramsValue[0]);
        if (victim != null && victim.IsOnlined)
        {
            player.message(victim.getName() + "'s account name is " + victim.getClient().getAccountName() + ".");
        }
        else
        {
            player.message("Player '" + paramsValue[0] + "' could not be found on this world.");
        }
    }
}
