namespace Application.Core.Game.Commands.Gm0;

public class ChangeLanguageCommand : CommandBase
{
    public ChangeLanguageCommand(): base(0, "changel")
    {

    }
    public override void Execute(IClient c, string[] values)
    {
        if (values.Length < 1)
        {
            c.OnlinedCharacter.yellowMessage("Syntax: !changel <0=ptb, 1=esp, 2=eng>");
            return;
        }
        c.setLanguage(int.Parse(values[0]));
    }
}
