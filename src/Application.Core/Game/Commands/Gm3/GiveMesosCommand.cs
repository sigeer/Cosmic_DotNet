namespace Application.Core.Game.Commands.Gm3;

public class GiveMesosCommand : CommandBase
{
    public GiveMesosCommand() : base(3, "givems")
    {
        Description = "Give mesos to a player.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !givems [<playername>] <gainmeso>");
            return;
        }

        string recv_, value_;
        long mesos_ = 0;

        if (paramsValue.Length == 2)
        {
            recv_ = paramsValue[0];
            value_ = paramsValue[1];
        }
        else
        {
            recv_ = c.OnlinedCharacter.getName();
            value_ = paramsValue[0];
        }

        try
        {
            mesos_ = long.Parse(value_);
            if (mesos_ > int.MaxValue)
            {
                mesos_ = int.MaxValue;
            }
            else if (mesos_ < int.MinValue)
            {
                mesos_ = int.MinValue;
            }
        }
        catch (FormatException)
        {
            if (value_ == ("max"))
            {  // "max" descriptor suggestion thanks to Vcoc
                mesos_ = int.MaxValue;
            }
            else if (value_ == ("min"))
            {
                mesos_ = int.MinValue;
            }
        }

        var victim = c.getWorldServer().getPlayerStorage().getCharacterByName(recv_);
        if (victim != null && victim.IsOnlined)
        {
            victim.gainMeso((int)mesos_, true);
            player.message("MESO given.");
        }
        else
        {
            player.message("Player '" + recv_ + "' could not be found.");
        }
    }
}
