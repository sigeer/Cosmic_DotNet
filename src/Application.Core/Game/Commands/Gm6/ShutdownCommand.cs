using net.server;
using server;

namespace Application.Core.Game.Commands.Gm6;

public class ShutdownCommand : CommandBase
{
    public ShutdownCommand() : base(6, "shutdown")
    {
        Description = "Shut down the server.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !shutdown [<time>|NOW]");
            return;
        }

        int time = 60000;
        if (paramsValue[0].Equals("now", StringComparison.OrdinalIgnoreCase))
        {
            time = 1;
        }
        else
        {
            time *= int.Parse(paramsValue[0]);
        }

        if (time > 1)
        {
            var dur = TimeSpan.FromMilliseconds(time);


            string strTime = "";
            if (dur.Days > 0)
            {
                strTime += dur.Days + " days, ";
            }
            if (dur.Hours > 0)
            {
                strTime += dur.Hours + " hours, ";
            }
            strTime += dur.Minutes + " minutes, ";
            strTime += dur.Seconds + " seconds";

            foreach (var w in Server.getInstance().getWorlds())
            {
                foreach (var chr in w.getPlayerStorage().GetAllOnlinedPlayers())
                {
                    chr.dropMessage("NewServer is undergoing maintenance process, and will be shutdown in " + strTime + ". Prepare yourself to quit safely in the mean time.");
                }
            }
        }

        TimerManager.getInstance().schedule(Server.getInstance().shutdown(false), time);
    }
}
