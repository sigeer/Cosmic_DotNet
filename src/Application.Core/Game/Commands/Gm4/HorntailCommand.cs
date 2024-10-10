namespace Application.Core.Game.Commands.Gm4;

public class HorntailCommand : CommandBase
{
    public HorntailCommand() : base(4, "horttail")
    {
        Description = "Spawn Horntail on your location.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        var targetPoint = player.getPosition();
        var targetMap = player.getMap();

        targetMap.spawnHorntailOnGroundBelow(targetPoint);
    }
}
