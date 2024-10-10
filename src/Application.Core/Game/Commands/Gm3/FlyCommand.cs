
using net.server;

namespace Application.Core.Game.Commands.Gm3;
public class FlyCommand : CommandBase
{
    public FlyCommand() : base(3, "fly")
    {
        Description = "Enable/disable fly feature.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    { // fly option will become available for any character of that account
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !fly <on/off>");
            return;
        }

        int accid = c.getAccID();
        Server srv = Server.getInstance();
        string sendStr = "";
        if (paramsValue[0].Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            sendStr += "Enabled Fly feature (F1). With fly active, you cannot attack.";
            if (!srv.canFly(accid))
            {
                sendStr += " Re-login to take effect.";
            }

            srv.changeFly(c.getAccID(), true);
        }
        else
        {
            sendStr += "Disabled Fly feature. You can now attack.";
            if (srv.canFly(accid))
            {
                sendStr += " Re-login to take effect.";
            }

            srv.changeFly(c.getAccID(), false);
        }

        player.dropMessage(6, sendStr);
    }
}
