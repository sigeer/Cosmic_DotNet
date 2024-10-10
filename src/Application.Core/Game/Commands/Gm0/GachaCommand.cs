using constants.id;
using server;
using server.gachapon;

namespace Application.Core.Game.Commands.Gm0;

public class GachaCommand : CommandBase
{
    public GachaCommand() : base(0, "gacha")
    {
        Description = "Show gachapon rewards.";
    }

    public override void Execute(IClient c, string[] paramValues)
    {
        Gachapon.GachaponType? gacha = null;
        string search = c.OnlinedCharacter.getLastCommandMessage();
        string gachaName = "";
        string[] names = { "Henesys", "Ellinia", "Perion", "Kerning City", "Sleepywood", "Mushroom Shrine", "Showa Spa Male", "Showa Spa Female", "New Leaf City", "Nautilus Harbor" };
        int[] ids = [
            NpcId.GACHAPON_HENESYS,
            NpcId.GACHAPON_ELLINIA,
            NpcId.GACHAPON_PERION,
            NpcId.GACHAPON_KERNING,
            NpcId.GACHAPON_SLEEPYWOOD,
            NpcId.GACHAPON_MUSHROOM_SHRINE,
            NpcId.GACHAPON_SHOWA_MALE,
            NpcId.GACHAPON_SHOWA_FEMALE,
            NpcId.GACHAPON_NLC,
            NpcId.GACHAPON_NAUTILUS];

        for (int i = 0; i < names.Length; i++)
        {
            if (search.Equals(names[i], StringComparison.OrdinalIgnoreCase))
            {
                gachaName = names[i];
                gacha = Gachapon.GachaponType.getByNpcId(ids[i]);
                break;
            }
        }

        if (gacha == null)
        {
            c.OnlinedCharacter.yellowMessage("Please use @gacha <name> where name corresponds to one of the below:");
            foreach (string name in names)
            {
                c.OnlinedCharacter.yellowMessage(name);
            }
            return;
        }
        string talkStr = "The #b" + gachaName + "#k Gachapon contains the following items.\r\n\r\n";
        for (int i = 0; i < 2; i++)
        {
            foreach (int id in gacha.getItems(i))
            {
                talkStr += "-" + ItemInformationProvider.getInstance().getName(id) + "\r\n";
            }
        }
        talkStr += "\r\nPlease keep in mind that there are items that are in all gachapons and are not listed here.";

        c.getAbstractPlayerInteraction().npcTalk(NpcId.MAPLE_ADMINISTRATOR, talkStr);
    }
}
