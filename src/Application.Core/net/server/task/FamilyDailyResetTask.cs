using Application.Core.Game.TheWorld;
using Application.Core.Managers;
using client;

namespace net.server.task;

public class FamilyDailyResetTask : AbstractRunnable
{
    private IWorld world;

    public FamilyDailyResetTask(IWorld world)
    {
        this.world = world;
    }

    public override void HandleRun()
    {
        FamilyManager.resetEntitlementUsage(world);
        foreach (Family family in world.getFamilies())
        {
            family.resetDailyReps();
        }
    }
}
