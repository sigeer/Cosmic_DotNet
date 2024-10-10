using server;

namespace Application.Core.Game.Commands.Gm2;

public class GmShopCommand : CommandBase
{
    public GmShopCommand() : base(2, "gmshop")
    {
        Description = "Open the GM shop.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        ShopFactory.getInstance().getShop(1337)!.sendShop(c);
    }
}
