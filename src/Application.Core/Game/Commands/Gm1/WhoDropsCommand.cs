using constants.id;
using server;
using server.life;

namespace Application.Core.Game.Commands.Gm1;

public class WhoDropsCommand : CommandBase
{
    public WhoDropsCommand() : base(1, "whodrops")
    {
        Description = "Show what drops an item.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        if (paramsValue.Length < 1)
        {
            player.dropMessage(5, "Please do @whodrops <item name>");
            return;
        }

        if (c.tryacquireClient())
        {
            try
            {
                string searchString = player.getLastCommandMessage();
                string output = "";
                var items = ItemInformationProvider.getInstance().getItemDataByName(searchString).Take(3);
                foreach (var data in items)
                {
                    output += "#b" + data.Name + "#k is dropped by:\r\n";

                    try
                    {
                        using var dbContext = new DBContext();
                        var ds = dbContext.DropData.Where(x => x.Itemid == data.Id).Take(50);

                        foreach (var item in ds)
                        {
                            string resultName = MonsterInformationProvider.getInstance().getMobNameFromId(item.Dropperid);
                            if (resultName != null)
                            {
                                output += resultName + ", ";
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        player.dropMessage(6, "There was a problem retrieving the required data. Please try again.");
                        log.Error(e.ToString());
                        return;
                    }
                    output += "\r\n\r\n";
                }
                c.getAbstractPlayerInteraction().npcTalk(NpcId.MAPLE_ADMINISTRATOR, output);
            }
            finally
            {
                c.releaseClient();
            }
        }
        else
        {
            player.dropMessage(5, "Please wait a while for your request to be processed.");
        }
    }
}
