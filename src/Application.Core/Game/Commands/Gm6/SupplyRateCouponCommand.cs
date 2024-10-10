namespace Application.Core.Game.Commands.Gm6;

public class SupplyRateCouponCommand : CommandBase
{
    public SupplyRateCouponCommand() : base(6, "supplyratecoupon")
    {
        Description = "Set availability of coupons in Cash Shop.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.dropMessage(5, "Syntax: !supplyratecoupon <yes|no>");
            return;
        }

        YamlConfig.config.server.USE_SUPPLY_RATE_COUPONS = !paramsValue[0].Equals("no", StringComparison.OrdinalIgnoreCase);
        player.dropMessage(5, "Rate coupons are now " + (YamlConfig.config.server.USE_SUPPLY_RATE_COUPONS ? "enabled" : "disabled") + " for purchase at the Cash Shop.");
    }
}
