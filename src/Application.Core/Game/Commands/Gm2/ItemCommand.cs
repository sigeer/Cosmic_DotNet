using Application.Core.Game.Players;
using Application.Core.Managers;
using Application.Core.scripting.npc;
using Application.Shared;
using client.inventory.manipulator;
using constants.id;
using constants.inventory;
using server;
using System.Text;

namespace Application.Core.Game.Commands.Gm2;

public class ItemCommand : CommandBase
{
    public ItemCommand() : base(2, "item")
    {
        Description = "Spawn an item into your inventory.";
    }

    public override void Execute(IClient c, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;

        if (paramsValue.Length < 1)
        {
            player.yellowMessage("Syntax: !item <itemid> <quantity>");
            return;
        }

        if (!int.TryParse(paramsValue[0], out var itemId))
        {
            var findResult = ResManager.FindItemIdByName(paramsValue[0]);
            if (findResult.BestMatch != null)
                itemId = findResult.BestMatch.Id;
            else if (findResult.MatchedItems.Count > 0)
            {
                var messages = new StringBuilder("找到了这些相似项：\r\n");
                for (int i = 0; i < findResult.MatchedItems.Count; i++)
                {
                    var item = findResult.MatchedItems[i];
                    messages.Append($"\r\n#L{i}# {item.Id} #t{item.Id}# - {item.Name} #l");
                }
                c.NPCConversationManager?.dispose();

                var tempConversation = new TempConversation(c, NpcId.MAPLE_ADMINISTRATOR);
                tempConversation.RegisterSelect(messages.ToString(), (idx, ctx) =>
                {
                    var item = findResult.MatchedItems[idx];
                    ctx.RegisterYesOrNo($"选择 {item.Id} #t{item.Id}# - {item.Name}？", ctx =>
                    {
                        SendItem(c, item.Id, paramsValue);
                        ctx.dispose();
                    });
                });
                c.NPCConversationManager = tempConversation;
                return;
            }
        }
        SendItem(c, itemId, paramsValue);
    }

    private void SendItem(IClient c, int itemId, string[] paramsValue)
    {
        var player = c.OnlinedCharacter;
        ItemInformationProvider ii = ItemInformationProvider.getInstance();

        if (ii.getName(itemId) == null)
        {
            player.yellowMessage("Item id '" + paramsValue[0] + "' does not exist.");
            return;
        }

        short quantity = 1;
        if (paramsValue.Length >= 2)
        {
            quantity = short.Parse(paramsValue[1]);
        }


        if (YamlConfig.config.server.BLOCK_GENERATE_CASH_ITEM && ii.isCash(itemId))
        {
            player.yellowMessage("You cannot create a cash item with this command.");
            return;
        }

        if (ItemConstants.isPet(itemId))
        {
            if (paramsValue.Length >= 2)
            {   
                // thanks to istreety & TacoBell
                quantity = 1;
                long days = Math.Max(1, int.Parse(paramsValue[1]));
                long expiration = DateTimeOffset.Now.AddDays(days).ToUnixTimeMilliseconds();
                int petid = ItemManager.CreatePet(itemId);

                InventoryManipulator.addById(c, itemId, quantity, player.getName(), petid, expiration: expiration);
                return;
            }
            else
            {
                player.yellowMessage("Pet Syntax: !item <itemid> <expiration>");
                return;
            }
        }

        short flag = 0;
        if (player.gmLevel() < 3)
        {
            flag |= ItemConstants.ACCOUNT_SHARING;
            flag |= ItemConstants.UNTRADEABLE;
        }

        InventoryManipulator.addById(c, itemId, quantity, player.getName(), -1, flag, -1);
    }
}
