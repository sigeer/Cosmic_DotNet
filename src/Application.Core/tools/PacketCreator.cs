/*
 This file is part of the OdinMS Maple Story Server
 Copyright (C) 2008 Patrick Huy <patrick.huy@frz.cc>
 Matthias Butz <matze@odinms.de>
 Jan Christian Meyer <vimes@odinms.de>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Affero General Public License as
 published by the Free Software Foundation version 3 as published by
 the Free Software Foundation. You may not use, modify or distribute
 this program under any other version of the GNU Affero General Public
 License.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Affero General Public License for more details.
 You should have received a copy of the GNU Affero General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */


using Application.Core.Game.Items;
using Application.Core.Game.Life;
using Application.Core.Game.Life.Monsters;
using Application.Core.Game.Maps;
using Application.Core.Game.Maps.AnimatedObjects;
using Application.Core.Game.Maps.Mists;
using Application.Core.Game.Relation;
using Application.Core.Game.Skills;
using Application.Core.Game.TheWorld;
using Application.Core.Managers;
using Application.Core.model;
using client;
using client.inventory;
using client.keybind;
using client.newyear;
using client.status;
using constants.game;
using constants.id;
using constants.inventory;
using constants.skills;
using net.encryption;
using net.opcodes;
using net.packet;
using net.server;
using net.server.channel.handlers;
using net.server.world;
using server;
using server.events.gm;
using server.life;
using server.maps;
using server.movement;
using System.Net;
using static Application.Core.Game.Maps.MiniGame;
using static client.inventory.Equip;
using static net.server.channel.handlers.AbstractDealDamageHandler;
using static net.server.channel.handlers.SummonDamageHandler;
using static server.CashShop;

namespace tools;


/**
 * @author Frz
 */
public class PacketCreator
{

    public static List<KeyValuePair<Stat, int>> EMPTY_STATUPDATE = [];
    // 2339/1/1 8:00:01
    private static long FT_UT_OFFSET = 116444736010800000L + TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).Ticks; // normalize with timezone offset suggested by Ari
    // 2448/1/1
    private static long DEFAULT_TIME = 150842304000000000L;//00 80 05 BB 46 E6 17 02
    // 2268/12/31
    public static long ZERO_TIME = 94354848000000000L;//00 40 E0 FD 3B 37 4F 01
    // 2447/12/31
    private static long PERMANENT = 150841440000000000L; // 00 C0 9B 90 7D E5 17 02

    public static long getTime(long utcTimestamp)
    {
        if (utcTimestamp < 0 && utcTimestamp >= -3)
        {
            if (utcTimestamp == -1)
            {
                return DEFAULT_TIME;    //high number ll
            }
            else if (utcTimestamp == -2)
            {
                return ZERO_TIME;
            }
            else
            {
                return PERMANENT;
            }
        }

        return utcTimestamp * 10000 + FT_UT_OFFSET;
    }

    private static void writeMobSkillId(OutPacket packet, MobSkillId msId)
    {
        packet.writeShort(msId.type.getId());
        packet.writeShort(msId.level);
    }

    public static Packet showHpHealed(int cid, int amount)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_FOREIGN_EFFECT);
        p.writeInt(cid);
        p.writeByte(0x0A); //Type
        p.writeByte(amount);
        return p;
    }

    private static void addRemainingSkillInfo(OutPacket p, IPlayer chr)
    {
        int[] remainingSp = chr.getRemainingSps();
        int effectiveLength = 0;
        foreach (int j in remainingSp)
        {
            if (j > 0)
            {
                effectiveLength++;
            }
        }

        p.writeByte(effectiveLength);
        for (int i = 0; i < remainingSp.Length; i++)
        {
            if (remainingSp[i] > 0)
            {
                p.writeByte(i + 1);
                p.writeByte(remainingSp[i]);
            }
        }
    }

    private static void addCharStats(OutPacket p, IPlayer chr)
    {
        p.writeInt(chr.getId()); // character id
        p.writeFixedString(chr.getName());
        p.writeByte(chr.getGender()); // gender (0 = male, 1 = female)
        p.writeByte((byte)chr.getSkinColor()); // skin color
        p.writeInt(chr.getFace()); // face
        p.writeInt(chr.getHair()); // hair

        for (int i = 0; i < 3; i++)
        {
            var pet = chr.getPet(i);
            if (pet != null) //Checked GMS.. and your pets stay when going into the cash shop.
            {
                p.writeLong(pet.getUniqueId());
            }
            else
            {
                p.writeLong(0);
            }
        }

        p.writeByte(chr.getLevel()); // level
        p.writeShort(chr.getJob().getId()); // job
        p.writeShort(chr.Str); // str
        p.writeShort(chr.Dex); // dex
        p.writeShort(chr.Int); // int
        p.writeShort(chr.Luk); // luk
        p.writeShort(chr.Hp); // hp (?)
        p.writeShort(chr.Maxhp); // maxhp
        p.writeShort(chr.Mp); // mp (?)
        p.writeShort(chr.Maxmp); // maxmp
        p.writeShort(chr.Ap); // remaining ap
        if (GameConstants.hasSPTable(chr.getJob()))
        {
            addRemainingSkillInfo(p, chr);
        }
        else
        {
            p.writeShort(chr.getRemainingSp()); // remaining sp
        }
        p.writeInt(chr.getExp()); // current exp
        p.writeShort(chr.getFame()); // fame
        p.writeInt(chr.getGachaExp()); //Gacha Exp
        p.writeInt(chr.getMapId()); // current map id
        p.writeByte(chr.getInitialSpawnpoint()); // spawnpoint
        p.writeInt(0);
    }

    protected static void addCharLook(OutPacket p, IPlayer chr, bool mega)
    {
        p.writeByte(chr.getGender());
        p.writeByte((int)chr.getSkinColor()); // skin color
        p.writeInt(chr.getFace()); // face
        p.writeBool(!mega);
        p.writeInt(chr.getHair()); // hair
        addCharEquips(p, chr);
    }

    private static void addCharacterInfo(OutPacket p, IPlayer chr)
    {
        p.writeLong(-1);
        p.writeByte(0);
        addCharStats(p, chr);
        p.writeByte(chr.BuddyList.Capacity);

        if (chr.getLinkedName() == null)
        {
            p.writeByte(0);
        }
        else
        {
            p.writeByte(1);
            p.writeString(chr.getLinkedName());
        }

        p.writeInt(chr.getMeso());
        addInventoryInfo(p, chr);
        addSkillInfo(p, chr);
        addQuestInfo(p, chr);
        addMiniGameInfo(p, chr);
        addRingInfo(p, chr);
        addTeleportInfo(p, chr);
        addMonsterBookInfo(p, chr);
        addNewYearInfo(p, chr);
        addAreaInfo(p, chr);//assuming it stayed here xd
        p.writeShort(0);
    }

    private static void addNewYearInfo(OutPacket p, IPlayer chr)
    {
        HashSet<NewYearCardRecord> received = chr.getReceivedNewYearRecords();

        p.writeShort(received.Count);
        foreach (NewYearCardRecord nyc in received)
        {
            encodeNewYearCard(nyc, p);
        }
    }

    private static void addTeleportInfo(OutPacket p, IPlayer chr)
    {
        List<int> tele = chr.getTrockMaps();
        List<int> viptele = chr.getVipTrockMaps();
        for (int i = 0; i < 5; i++)
        {
            p.writeInt(tele[i]);
        }
        for (int i = 0; i < 10; i++)
        {
            p.writeInt(viptele[i]);
        }
    }

    private static void addMiniGameInfo(OutPacket p, IPlayer chr)
    {
        p.writeShort(0);
        /*foreach(int m = size; m > 0; m--) {//nexon does this in P
         p.writeInt(0);
         p.writeInt(0);
         p.writeInt(0);
         p.writeInt(0);
         p.writeInt(0);
         }*/
    }

    private static void addAreaInfo(OutPacket p, IPlayer chr)
    {
        Dictionary<short, string> areaInfos = chr.getAreaInfos();
        p.writeShort(areaInfos.Count);
        foreach (short area in areaInfos.Keys)
        {
            p.writeShort(area);
            p.writeString(areaInfos[area]);
        }
    }

    private static void addCharEquips(OutPacket p, IPlayer chr)
    {
        Inventory equip = chr.getInventory(InventoryType.EQUIPPED);
        var ii = ItemInformationProvider.getInstance().canWearEquipment(chr, equip.list());
        Dictionary<short, int> myEquip = new();
        Dictionary<short, int> maskedEquip = new();
        foreach (Item item in ii)
        {
            short pos = (byte)(item.getPosition() * -1);
            if (pos < 100 && myEquip.get(pos) == null)
            {
                myEquip.AddOrUpdate(pos, item.getItemId());
            }
            else if (pos > 100 && pos != 111)
            { // don't ask. o.o
                pos -= 100;
                if (myEquip.get(pos) != null)
                {
                    maskedEquip.AddOrUpdate(pos, myEquip.GetValueOrDefault(pos));
                }
                myEquip.AddOrUpdate(pos, item.getItemId());
            }
            else if (myEquip.get(pos) != null)
            {
                maskedEquip.AddOrUpdate(pos, item.getItemId());
            }
        }
        foreach (var entry in myEquip)
        {
            p.writeByte(entry.Key);
            p.writeInt(entry.Value);
        }
        p.writeByte(0xFF);
        foreach (var entry in maskedEquip)
        {
            p.writeByte(entry.Key);
            p.writeInt(entry.Value);
        }
        p.writeByte(0xFF);
        var cWeapon = equip.getItem(-111);
        p.writeInt(cWeapon != null ? cWeapon.getItemId() : 0);
        for (int i = 0; i < 3; i++)
        {
            if (chr.getPet(i) != null)
            {
                p.writeInt(chr.getPet(i)!.getItemId());
            }
            else
            {
                p.writeInt(0);
            }
        }
    }

    public static Packet setExtraPendantSlot(bool toggleExtraSlot)
    {
        OutPacket p = OutPacket.create(SendOpcode.SET_EXTRA_PENDANT_SLOT);
        p.writeBool(toggleExtraSlot);
        return p;
    }

    private static void addCharEntry(OutPacket p, IPlayer chr, bool viewall)
    {
        addCharStats(p, chr);
        addCharLook(p, chr, false);
        if (!viewall)
        {
            p.writeByte(0);
        }
        if (chr.isGM() || chr.isGmJob())
        {  // thanks Daddy Egg (Ubaware), resinate for noticing GM jobs crashing on non-GM players account
            p.writeByte(0);
            return;
        }
        p.writeByte(1); // world rank enabled (next 4 ints are not sent if disabled) short??
        p.writeInt(chr.getRank()); // world rank
        p.writeInt(chr.getRankMove()); // move (negative is downwards)
        p.writeInt(chr.getJobRank()); // job rank
        p.writeInt(chr.getJobRankMove()); // move (negative is downwards)
    }

    private static void addQuestInfo(OutPacket p, IPlayer chr)
    {
        List<QuestStatus> started = chr.getStartedQuests();
        int startedSize = 0;
        foreach (QuestStatus qs in started)
        {
            if (qs.getInfoNumber() > 0)
            {
                startedSize++;
            }
            startedSize++;
        }
        p.writeShort(startedSize);
        foreach (QuestStatus qs in started)
        {
            p.writeShort(qs.getQuest().getId());
            p.writeString(qs.getProgressData());

            short infoNumber = qs.getInfoNumber();
            if (infoNumber > 0)
            {
                QuestStatus iqs = chr.getQuest(infoNumber);
                p.writeShort(infoNumber);
                p.writeString(iqs.getProgressData());
            }
        }
        List<QuestStatus> completed = chr.getCompletedQuests();
        p.writeShort(completed.Count);
        foreach (QuestStatus qs in completed)
        {
            p.writeShort(qs.getQuest().getId());
            p.writeLong(getTime(qs.getCompletionTime()));
        }
    }

    private static void addExpirationTime(OutPacket p, long time)
    {
        p.writeLong(getTime(time)); // offset expiration time issue found thanks to Thora
    }

    private static void addItemInfo(OutPacket p, Item item)
    {
        addItemInfo(p, item, false);
    }

    protected static void addItemInfo(OutPacket p, Item item, bool zeroPosition)
    {
        ItemInformationProvider ii = ItemInformationProvider.getInstance();
        bool isCash = ii.isCash(item.getItemId());
        bool isPet = item.getPetId() > -1;
        bool isRing = false;
        Equip? equip = null;
        short pos = item.getPosition();
        sbyte itemType = item.getItemType();
        if (itemType == 1)
        {
            equip = (Equip)item;
            isRing = equip.getRingId() > -1;
        }
        if (!zeroPosition)
        {
            if (equip != null)
            {
                if (pos < 0)
                {
                    pos *= -1;
                }
                p.writeShort(pos > 100 ? pos - 100 : pos);
            }
            else
            {
                p.writeByte(pos);
            }
        }
        p.writeByte(itemType);
        p.writeInt(item.getItemId());
        p.writeBool(isCash);
        if (isCash)
        {
            p.writeLong(isPet ? item.getPetId() : isRing ? equip.getRingId() : item.getCashId());
        }
        addExpirationTime(p, item.getExpiration());
        if (isPet)
        {
            Pet pet = item.getPet()!;
            p.writeFixedString(pet.getName());
            p.writeByte(pet.Level);
            p.writeShort(pet.Tameness);
            p.writeByte(pet.Fullness);
            addExpirationTime(p, item.getExpiration());
            p.writeShort(pet.getPetAttribute()); // PetAttribute noticed by lrenex & Spoon
            p.writeShort(0); // PetSkill
            p.writeInt(18000); // RemainLife
            p.writeShort(0); // attribute
            return;
        }
        if (equip == null)
        {
            p.writeShort(item.getQuantity());
            p.writeString(item.getOwner());
            p.writeShort(item.getFlag()); // flag

            if (ItemConstants.isRechargeable(item.getItemId()))
            {
                p.writeInt(2);
                p.writeBytes(new byte[] { 0x54, 0, 0, 0x34 });
            }
            return;
        }
        p.writeByte(equip.getUpgradeSlots()); // upgrade slots
        p.writeByte(equip.getLevel()); // level
        p.writeShort(equip.getStr()); // str
        p.writeShort(equip.getDex()); // dex
        p.writeShort(equip.getInt()); // int
        p.writeShort(equip.getLuk()); // luk
        p.writeShort(equip.getHp()); // hp
        p.writeShort(equip.getMp()); // mp
        p.writeShort(equip.getWatk()); // watk
        p.writeShort(equip.getMatk()); // matk
        p.writeShort(equip.getWdef()); // wdef
        p.writeShort(equip.getMdef()); // mdef
        p.writeShort(equip.getAcc()); // accuracy
        p.writeShort(equip.getAvoid()); // avoid
        p.writeShort(equip.getHands()); // hands
        p.writeShort(equip.getSpeed()); // speed
        p.writeShort(equip.getJump()); // jump
        p.writeString(equip.getOwner()); // owner name
        p.writeShort(equip.getFlag()); //Item Flags

        if (isCash)
        {
            for (int i = 0; i < 10; i++)
            {
                p.writeByte(0x40);
            }
        }
        else
        {
            int itemLevel = equip.getItemLevel();

            long expNibble = (ExpTable.getExpNeededForLevel(ii.getEquipLevelReq(item.getItemId())) * equip.getItemExp());
            expNibble /= ExpTable.getEquipExpNeededForLevel(itemLevel);

            p.writeByte(0);
            p.writeByte(itemLevel); //Item Level
            p.writeInt((int)expNibble);
            p.writeInt(equip.getVicious()); //WTF NEXON ARE YOU SERIOUS?
            p.writeLong(0);
        }
        p.writeLong(getTime(-2));
        p.writeInt(-1);

    }

    private static void addInventoryInfo(OutPacket p, IPlayer chr)
    {
        for (sbyte i = 1; i <= 5; i++)
        {
            p.writeByte(chr.getInventory(InventoryTypeUtils.getByType(i)).getSlotLimit());
        }
        p.writeLong(getTime(-2));
        Inventory iv = chr.getInventory(InventoryType.EQUIPPED);
        var equippedC = iv.list();
        List<Item> equipped = new(equippedC.Count);
        List<Item> equippedCash = new(equippedC.Count);
        foreach (Item item in equippedC)
        {
            if (item.getPosition() <= -100)
            {
                equippedCash.Add(item);
            }
            else
            {
                equipped.Add(item);
            }
        }
        foreach (Item item in equipped)
        {    // equipped doesn't actually need sorting, thanks Pllsz
            addItemInfo(p, item);
        }
        p.writeShort(0); // start of equip cash
        foreach (Item item in equippedCash)
        {
            addItemInfo(p, item);
        }
        p.writeShort(0); // start of equip inventory
        foreach (Item item in chr.getInventory(InventoryType.EQUIP).list())
        {
            addItemInfo(p, item);
        }
        p.writeInt(0);
        foreach (Item item in chr.getInventory(InventoryType.USE).list())
        {
            addItemInfo(p, item);
        }
        p.writeByte(0);
        foreach (Item item in chr.getInventory(InventoryType.SETUP).list())
        {
            addItemInfo(p, item);
        }
        p.writeByte(0);
        foreach (Item item in chr.getInventory(InventoryType.ETC).list())
        {
            addItemInfo(p, item);
        }
        p.writeByte(0);
        foreach (Item item in chr.getInventory(InventoryType.CASH).list())
        {
            addItemInfo(p, item);
        }
    }

    private static void addSkillInfo(OutPacket p, IPlayer chr)
    {
        p.writeByte(0); // start of skills
        var skills = chr.getSkills();
        int skillsSize = skills.Count;
        // We don't want to include any hidden skill in this, so subtract them from the size list and ignore them.
        foreach (var skill in skills)
        {
            if (GameConstants.isHiddenSkills(skill.Key.getId()))
            {
                skillsSize--;
            }
        }
        p.writeShort(skillsSize);
        foreach (var skill in skills)
        {
            if (GameConstants.isHiddenSkills(skill.Key.getId()))
            {
                continue;
            }
            p.writeInt(skill.Key.getId());
            p.writeInt(skill.Value.skillevel);
            addExpirationTime(p, skill.Value.expiration);
            if (skill.Key.isFourthJob())
            {
                p.writeInt(skill.Value.masterlevel);
            }
        }
        p.writeShort(chr.getAllCooldowns().Count);
        foreach (PlayerCoolDownValueHolder cooling in chr.getAllCooldowns())
        {
            p.writeInt(cooling.skillId);
            int timeLeft = (int)(cooling.length + cooling.startTime - DateTimeOffset.Now.ToUnixTimeMilliseconds());
            p.writeShort(timeLeft / 1000);
        }
    }

    private static void addMonsterBookInfo(OutPacket p, IPlayer chr)
    {
        p.writeInt(chr.getMonsterBookCover()); // cover
        p.writeByte(0);
        Dictionary<int, int> cards = chr.getMonsterBook().getCards();
        p.writeShort(cards.Count);
        foreach (var all in cards)
        {
            p.writeShort(all.Key % 10000); // Id
            p.writeByte(all.Value); // Level
        }
    }

    public static Packet sendGuestTOS()
    {
        OutPacket p = OutPacket.create(SendOpcode.GUEST_ID_LOGIN);
        p.writeShort(0x100);
        p.writeInt(Randomizer.nextInt(999999));
        p.writeLong(0);
        p.writeLong(getTime(-2));
        p.writeLong(getTime(DateTimeOffset.Now.ToUnixTimeMilliseconds()));
        p.writeInt(0);
        p.writeString("http://maplesolaxia.com");
        return p;
    }

    /**
     * Sends a hello packet.
     *
     * @param mapleVersion The maple client version.
     * @param sendIv       the IV in use by the server for sending
     * @param recvIv       the IV in use by the server for receiving
     * @return
     */
    public static Packet getHello(short mapleVersion, InitializationVector sendIv, InitializationVector recvIv)
    {
        OutPacket p = new ByteBufOutPacket();
        p.writeShort(0x0E);
        p.writeShort(mapleVersion);
        p.writeShort(1);
        p.writeByte(49);
        p.writeBytes(recvIv.getBytes());
        p.writeBytes(sendIv.getBytes());
        p.writeByte(8);
        return p;
    }

    /**
     * Sends a ping packet.
     *
     * @return The packet.
     */
    public static Packet getPing()
    {
        return OutPacket.create(SendOpcode.PING);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reason">
    /// The reason logging in failed.
    /// <para>Possible values for <paramref name="reason"/>:</para>
    /// <para>3: ID deleted or blocked</para>
    /// <para>4: Incorrect password</para>
    /// <para>5: Not a registered id</para>
    /// <para>6: System error</para>
    /// <para>7: Already logged in</para>
    /// <para>8: System error</para>
    /// <para>9: System error</para>
    /// <para>10: Cannot process so many connections</para>
    /// <para>11: Only users older than 20 can use this channel</para>
    /// <para>13: Unable to log on as master at this ip</para>
    /// <para>14: Wrong gateway or personal info and weird korean button</para>
    /// <para>15: Processing request with that korean button!</para>
    /// <para>16: Please verify your account through email...</para>
    /// <para>17: Wrong gateway or personal info</para>
    /// <para>21: Please verify your account through email...</para>
    /// <para>23: License agreement</para>
    /// <para>25: Maple Europe notice =[ FUCK YOU NEXON</para>
    /// <para>27: Some weird full client  notice, probably for trial versions</para>
    /// </param>
    /// <returns>The login failed packet.</returns>
    public static Packet getLoginFailed(int reason)
    {
        OutPacket p = OutPacket.create(SendOpcode.LOGIN_STATUS);
        p.writeByte(reason);
        p.writeByte(0);
        p.writeInt(0);
        return p;
    }

    /// <summary>
    /// Gets a login failed packet.
    /// </summary>
    /// <param name="reason">
    /// The reason logging in failed.
    /// <para>Possible values for <paramref name="reason"/>:</para>
    /// <para>3: ID deleted or blocked</para>
    /// <para>4: Incorrect password</para>
    /// <para>5: Not a registered id</para>
    /// <para>6: Trouble logging into the game?</para>
    /// <para>7: Already logged in</para>
    /// <para>8: Trouble logging into the game?</para>
    /// <para>9: Trouble logging into the game?</para>
    /// <para>10: Cannot process so many connections</para>
    /// <para>11: Only users older than 20 can use this channel</para>
    /// <para>12: Trouble logging into the game?</para>
    /// <para>13: Unable to log on as master at this ip</para>
    /// <para>14: Wrong gateway or personal info and weird korean button</para>
    /// <para>15: Processing request with that korean button!</para>
    /// <para>16: Please verify your account through email...</para>
    /// <para>17: Wrong gateway or personal info</para>
    /// <para>21: Please verify your account through email...</para>
    /// <para>23: Crashes</para>
    /// <para>25: Maple Europe notice =[ FUCK YOU NEXON</para>
    /// <para>27: Some weird full client  notice, probably for trial versions</para>
    /// </param>
    /// <returns>The login failed packet.</returns>
    public static Packet getAfterLoginError(int reason)
    {//same as above o.o
        OutPacket p = OutPacket.create(SendOpcode.SELECT_CHARACTER_BY_VAC);
        p.writeShort(reason);//using other types than stated above = CRASH
        return p;
    }

    public static Packet sendPolice()
    {
        OutPacket p = OutPacket.create(SendOpcode.FAKE_GM_NOTICE);
        p.writeByte(0);//doesn't even matter what value
        return p;
    }

    public static Packet sendPolice(string text)
    {
        OutPacket p = OutPacket.create(SendOpcode.DATA_CRC_CHECK_FAILED);
        p.writeString(text);
        return p;
    }

    public static Packet getPermBan(byte reason)
    {
        OutPacket p = OutPacket.create(SendOpcode.LOGIN_STATUS);
        p.writeByte(2); // Account is banned
        p.writeByte(0);
        p.writeInt(0);
        p.writeByte(0);
        p.writeLong(getTime(-1));
        return p;
    }

    public static Packet getTempBan(long timestampTill, byte reason)
    {
        OutPacket p = OutPacket.create(SendOpcode.LOGIN_STATUS);
        p.writeByte(2);
        p.writeByte(0);
        p.writeInt(0);
        p.writeByte(reason);
        p.writeLong(getTime(timestampTill)); // Tempban date is handled as a 64-bit long, number of 100NS intervals since 1/1/1601. Lulz.
        return p;
    }

    /**
     * Gets a successful authentication packet.
     *
     * @param c
     * @return the successful authentication packet
     */
    public static Packet getAuthSuccess(IClient c)
    {
        Server.getInstance().loadAccountCharacters(c);    // locks the login session until data is recovered from the cache or the DB.
        Server.getInstance().loadAccountStorages(c);

        OutPacket p = OutPacket.create(SendOpcode.LOGIN_STATUS);
        p.writeInt(0);
        p.writeShort(0);
        p.writeInt(c.getAccID());
        p.writeByte(c.getGender());

        bool canFly = Server.getInstance().canFly(c.getAccID());
        p.writeBool((YamlConfig.config.server.USE_ENFORCE_ADMIN_ACCOUNT || canFly) && c.getGMLevel() > 1);    // thanks Steve(kaito1410) for pointing the GM account bool here
        p.writeByte(((YamlConfig.config.server.USE_ENFORCE_ADMIN_ACCOUNT || canFly) && c.getGMLevel() > 1) ? 0x80 : 0);  // Admin Byte. 0x80,0x40,0x20.. Rubbish.
        p.writeByte(0); // Country Code.

        p.writeString(c.getAccountName());
        p.writeByte(0);

        p.writeByte(0); // IsQuietBan
        p.writeLong(0);//IsQuietBanTimeStamp
        p.writeLong(0); //CreationTimeStamp

        p.writeInt(1); // 1: Remove the "Select the world you want to play in"

        p.writeByte(YamlConfig.config.server.ENABLE_PIN && !c.canBypassPin() ? 0 : 1); // 0 = Pin-System Enabled, 1 = Disabled
        p.writeByte(YamlConfig.config.server.ENABLE_PIC && !c.canBypassPic() ? (c.getPic() == null || c.getPic().Equals("") ? 0 : 1) : 2); // 0 = Register PIC, 1 = Ask for PIC, 2 = Disabled

        return p;
    }

    /**
     * Gets a packet detailing a PIN operation.
     * <p>
     * Possible values for <code>mode</code>:<br> 0 - PIN was accepted<br> 1 -
     * Register a new PIN<br> 2 - Invalid pin / Reenter<br> 3 - Connection
     * failed due to system error<br> 4 - Enter the pin
     *
     * @param mode The mode.
     * @return
     */
    private static Packet pinOperation(byte mode)
    {
        OutPacket p = OutPacket.create(SendOpcode.CHECK_PINCODE);
        p.writeByte(mode);
        return p;
    }

    public static Packet pinRegistered()
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_PINCODE);
        p.writeByte(0);
        return p;
    }

    public static Packet requestPin()
    {
        return pinOperation(4);
    }

    public static Packet requestPinAfterFailure()
    {
        return pinOperation(2);
    }

    public static Packet registerPin()
    {
        return pinOperation(1);
    }

    public static Packet pinAccepted()
    {
        return pinOperation(0);
    }

    public static Packet wrongPic()
    {
        OutPacket p = OutPacket.create(SendOpcode.CHECK_SPW_RESULT);
        p.writeByte(0);
        return p;
    }

    /// <summary>
    /// Gets a packet detailing a server and its channels.
    /// </summary>
    /// <param name="serverId"></param>
    /// <param name="serverName">The name of the server.</param>
    /// <param name="flag"></param>
    /// <param name="eventmsg"></param>
    /// <param name="channelLoad">Load of the channel - 1200 seems to be max.</param>
    /// <returns>The server info packet.</returns>
    public static Packet getServerList(int serverId, string serverName, int flag, string eventmsg, List<IWorldChannel> channelLoad)
    {
        OutPacket p = OutPacket.create(SendOpcode.SERVERLIST);
        p.writeByte(serverId);
        p.writeString(serverName);
        p.writeByte(flag);
        p.writeString(eventmsg);
        p.writeByte(100); // rate modifier, don't ask O.O!
        p.writeByte(0); // event xp * 2.6 O.O!
        p.writeByte(100); // rate modifier, don't ask O.O!
        p.writeByte(0); // drop rate * 2.6
        p.writeByte(0);
        p.writeByte(channelLoad.Count);
        foreach (var ch in channelLoad)
        {
            p.writeString(serverName + "-" + ch.getId());
            p.writeInt(ch.getChannelCapacity());

            // thanks GabrielSin for this channel packet structure part
            p.writeByte(1);// nWorldID
            p.writeByte(ch.getId() - 1);// nChannelID
            p.writeBool(false);// bAdultChannel
        }
        p.writeShort(0);
        return p;
    }

    /// <summary>
    /// Gets a packet saying that the server list is over.
    /// </summary>
    /// <returns>The end of server list packet.</returns>
    public static Packet getEndOfServerList()
    {
        OutPacket p = OutPacket.create(SendOpcode.SERVERLIST);
        p.writeByte(0xFF);
        return p;
    }


    /// <summary>
    /// Gets a packet detailing a server status message.
    /// </summary>
    /// <param name="status">The server status.
    /// <para>Possible values for <paramref name="status"/>:</para>
    /// <para>0 - Normal</para>
    /// <para>1 - Highly populated</para>
    /// <para>2 - Full</para>
    /// </param>
    /// <returns>The server status packet.</returns>
    public static Packet getServerStatus(int status)
    {
        OutPacket p = OutPacket.create(SendOpcode.SERVERSTATUS);
        p.writeShort(status);
        return p;
    }

    /// <summary>
    /// Gets a packet telling the client the IP of the channel server.
    /// </summary>
    /// <param name="inetAddr">The InetAddress of the requested channel server.</param>
    /// <param name="port">The port the channel is on.</param>
    /// <param name="clientId">The ID of the client.</param>
    /// <returns>The server IP packet.</returns>
    public static Packet getServerIP(IPAddress inetAddr, int port, int clientId)
    {
        OutPacket p = OutPacket.create(SendOpcode.SERVER_IP);
        p.writeShort(0);
        byte[] addr = inetAddr.GetAddressBytes();
        p.writeBytes(addr);
        p.writeShort(port);
        p.writeInt(clientId);
        p.writeBytes(new byte[] { 0, 0, 0, 0, 0 });
        return p;
    }


    /// <summary>
    /// Gets a packet telling the client the IP of the new channel.
    /// </summary>
    /// <param name="inetAddr">The InetAddress of the requested channel server.</param>
    /// <param name="port">The port the channel is on.</param>
    /// <returns>The server IP packet.</returns>
    public static Packet getChannelChange(IPAddress inetAddr, int port)
    {
        OutPacket p = OutPacket.create(SendOpcode.CHANGE_CHANNEL);
        p.writeByte(1);
        byte[] addr = inetAddr.GetAddressBytes();
        p.writeBytes(addr);
        p.writeShort(port);
        return p;
    }

    /// <summary>
    ///  Gets a packet with a list of characters.
    /// </summary>
    /// <param name="c">The IClient to load characters of.</param>
    /// <param name="serverId">The ID of the server requested.</param>
    /// <param name="status">The charlist request result.
    /// Possible values for <paramref name="status"/>:
    /// <para> 2: ID deleted or blocked</para>
    /// <para> 3: ID deleted or blocked</para>
    /// <para> 4: Incorrect password</para>
    /// <para> 5: Not an registered ID</para>
    /// <para> 6: Trouble logging in?</para>
    /// <para> 10: Server handling too many connections</para>
    /// <para> 11: Only 20 years or older</para>
    /// <para> 13: Unable to log as master at IP</para>
    /// <para> 14: Wrong gateway or personal info</para>
    /// <para> 15: Still processing request</para>
    /// <para> 16: Verify account via email</para>
    /// <para> 17: Wrong gateway or personal info</para>
    /// <para> 21: Verify account via email</para>
    /// </param>
    /// <returns>The character list packet.</returns>
    public static Packet getCharList(IClient c, int serverId, int status)
    {
        OutPacket p = OutPacket.create(SendOpcode.CHARLIST);
        p.writeByte(status);
        List<IPlayer> chars = c.loadCharacters(serverId);
        p.writeByte((byte)chars.Count);
        foreach (IPlayer chr in chars)
        {
            addCharEntry(p, chr, false);
        }

        p.writeByte(YamlConfig.config.server.ENABLE_PIC && !c.canBypassPic() ? (string.IsNullOrEmpty(c.getPic()) ? 0 : 1) : 2);
        p.writeInt(YamlConfig.config.server.COLLECTIVE_CHARSLOT ? chars.Count + c.getAvailableCharacterSlots() : c.getCharacterSlots());
        return p;
    }

    public static Packet enableTV()
    {
        OutPacket p = OutPacket.create(SendOpcode.ENABLE_TV);
        p.writeInt(0);
        p.writeByte(0);
        return p;
    }


    /// <summary>
    /// Removes TV
    /// </summary>
    /// <returns>The Remove TV Packet</returns>
    public static Packet removeTV()
    {
        return OutPacket.create(SendOpcode.REMOVE_TV);
    }

    /// <summary>
    /// Sends MapleTV
    /// </summary>
    /// <param name="chr">The character shown in TV</param>
    /// <param name="messages">The message sent with the TV</param>
    /// <param name="type">The type of TV
    /// <para>0 - Normal</para>
    /// <para>1 - Star</para>
    /// <para>2 - Heart</para>
    /// </param>
    /// <param name="partner">The partner shown with chr</param>
    /// <returns>the SEND_TV packet</returns>
    public static Packet sendTV(IPlayer chr, List<string> messages, int type, IPlayer partner)
    {
        OutPacket p = OutPacket.create(SendOpcode.SEND_TV);
        p.writeByte(partner != null ? 3 : 1);
        p.writeByte(type); //Heart = 2  Star = 1  Normal = 0
        addCharLook(p, chr, false);
        p.writeString(chr.getName());
        if (partner != null)
        {
            p.writeString(partner.getName());
        }
        else
        {
            p.writeShort(0);
        }
        for (int i = 0; i < messages.Count; i++)
        {
            var messageItem = messages[i];
            if (i == 4 && messageItem.Length > 15)
            {
                p.writeString(messageItem.Substring(0, 15));
            }
            else
            {
                p.writeString(messageItem);
            }
        }
        p.writeInt(1337); // time limit shit lol 'Your thing still start in blah blah seconds'
        if (partner != null)
        {
            addCharLook(p, partner, false);
        }
        return p;
    }

    /// <summary>
    /// Gets character info for a character.
    /// </summary>
    /// <param name="chr">The character to get info about.</param>
    /// <returns>The character info packet.</returns>
    public static Packet getCharInfo(IPlayer chr)
    {
        OutPacket p = OutPacket.create(SendOpcode.SET_FIELD);
        p.writeInt(chr.getClient().getChannel() - 1);
        p.writeByte(1);
        p.writeByte(1);
        p.writeShort(0);
        for (int i = 0; i < 3; i++)
        {
            p.writeInt(Randomizer.nextInt());
        }
        addCharacterInfo(p, chr);
        p.writeLong(getTime(DateTimeOffset.Now.ToUnixTimeMilliseconds()));
        return p;
    }

    /// <summary>
    /// Gets an empty stat update.
    /// </summary>
    /// <returns>The empty stat update packet.</returns>
    public static Packet enableActions()
    {
        return updatePlayerStats(EMPTY_STATUPDATE, true, null);
    }

    /// <summary>
    /// Gets an update for specified stats.
    /// </summary>
    /// <param name="stats">The list of stats to update.</param>
    /// <param name="enableActions">Allows actions after the update.</param>
    /// <param name="chr">The update target.</param>
    /// <returns>The stat update packet.</returns>
    public static Packet updatePlayerStats(List<KeyValuePair<Stat, int>> stats, bool enableActions, IPlayer? chr)
    {
        OutPacket p = OutPacket.create(SendOpcode.STAT_CHANGED);
        p.writeBool(enableActions);
        int updateMask = 0;
        foreach (var statupdate in stats)
        {
            updateMask |= statupdate.Key.getValue();
        }
        var mystats = stats.OrderBy(x => x.Key.getValue()).ToList();
        p.writeInt(updateMask);
        foreach (var statupdate in mystats)
        {
            if (statupdate.Key.getValue() >= 1)
            {
                if (statupdate.Key.getValue() == 0x1)
                {
                    p.writeByte(statupdate.Value);
                }
                else if (statupdate.Key.getValue() <= 0x4)
                {
                    p.writeInt(statupdate.Value);
                }
                else if (statupdate.Key.getValue() < 0x20)
                {
                    p.writeByte(statupdate.Value);
                }
                else if (statupdate.Key.getValue() == 0x8000)
                {
                    if (GameConstants.hasSPTable(chr!.getJob()))
                    {
                        addRemainingSkillInfo(p, chr);
                    }
                    else
                    {
                        p.writeShort(statupdate.Value);
                    }
                }
                else if (statupdate.Key.getValue() < 0xFFFF)
                {
                    p.writeShort(statupdate.Value);
                }
                else if (statupdate.Key.getValue() == 0x20000)
                {
                    p.writeShort(statupdate.Value);
                }
                else
                {
                    p.writeInt(statupdate.Value);
                }
            }
        }
        return p;
    }

    /// <summary>
    /// Gets a packet telling the client to change maps.
    /// </summary>
    /// <param name="to">The <see cref="MapleMap"/> to warp to.</param>
    /// <param name="spawnPoint">The spawn portal number to spawn at.</param>
    /// <param name="chr">The character warping to <paramref name="to"/></param>
    /// <returns>The map change packet.</returns>
    public static Packet getWarpToMap(IMap to, int spawnPoint, IPlayer chr)
    {
        OutPacket p = OutPacket.create(SendOpcode.SET_FIELD);
        p.writeInt(chr.getClient().getChannel() - 1);
        p.writeInt(0);//updated
        p.writeByte(0);//updated
        p.writeInt(to.getId());
        p.writeByte(spawnPoint);
        p.writeShort(chr.getHp());
        p.writeBool(chr.isChasing());
        if (chr.isChasing())
        {
            chr.setChasing(false);
            p.writeInt(chr.getPosition().X);
            p.writeInt(chr.getPosition().Y);
        }
        p.writeLong(getTime(Server.getInstance().getCurrentTime()));
        return p;
    }

    public static Packet getWarpToMap(IMap to, int spawnPoint, Point spawnPosition, IPlayer chr)
    {
        OutPacket p = OutPacket.create(SendOpcode.SET_FIELD);
        p.writeInt(chr.getClient().getChannel() - 1);
        p.writeInt(0);//updated
        p.writeByte(0);//updated
        p.writeInt(to.getId());
        p.writeByte(spawnPoint);
        p.writeShort(chr.getHp());
        p.writeBool(true);
        p.writeInt(spawnPosition.X);    // spawn position placement thanks to Arnah (Vertisy)
        p.writeInt(spawnPosition.Y);
        p.writeLong(getTime(Server.getInstance().getCurrentTime()));
        return p;
    }

    /// <summary>
    /// Gets a packet to spawn a portal.
    /// </summary>
    /// <param name="townId">The ID of the town the portal goes to.</param>
    /// <param name="targetId">The ID of the target.</param>
    /// <param name="pos">Where to put the portal.</param>
    /// <returns>The portal spawn packet.</returns>
    public static Packet spawnPortal(int townId, int targetId, Point pos)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_PORTAL);
        p.writeInt(townId);
        p.writeInt(targetId);
        p.writePos(pos);
        return p;
    }

    /**
     * Gets a packet to spawn a door.
     *
     * @param ownerid  The door's owner ID.
     * @param pos      The position of the door.
     * @param launched Already deployed the door.
     * @return The remove door packet.
     */
    public static Packet spawnDoor(int ownerid, Point pos, bool launched)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_DOOR);
        p.writeBool(launched);
        p.writeInt(ownerid);
        p.writePos(pos);
        return p;
    }

    /**
     * Gets a packet to remove a door.
     *
     * @param ownerId The door's owner ID.
     * @param town
     * @return The remove door packet.
     */
    public static Packet removeDoor(int ownerId, bool town)
    {
        OutPacket p;
        if (town)
        {
            p = OutPacket.create(SendOpcode.SPAWN_PORTAL);
            p.writeInt(MapId.NONE);
            p.writeInt(MapId.NONE);
        }
        else
        {
            p = OutPacket.create(SendOpcode.REMOVE_DOOR);
            p.writeByte(0);
            p.writeInt(ownerId);
        }
        return p;
    }

    /**
     * Gets a packet to spawn a special map object.
     *
     * @param summon
     * @param skillLevel The level of the skill used.
     * @param animated   Animated spawn?
     * @return The spawn packet for the map object.
     */
    public static Packet spawnSummon(Summon summon, bool animated)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_SPECIAL_MAPOBJECT);
        p.writeInt(summon.getOwner().getId());
        p.writeInt(summon.getObjectId());
        p.writeInt(summon.getSkill());
        p.writeByte(0x0A); //v83
        p.writeByte(summon.getSkillLevel());
        p.writePos(summon.getPosition());
        p.writeByte(summon.getStance());    //bMoveAction & foothold, found thanks to Rien dev team
        p.writeShort(0);
        p.writeByte(summon.getMovementType().getValue()); // 0 = don't move, 1 = follow (4th mage summons?), 2/4 = only tele follow, 3 = bird follow
        p.writeBool(!summon.isPuppet()); // 0 and the summon can't attack - but puppets don't attack with 1 either ^.-
        p.writeBool(!animated);
        return p;
    }

    /**
     * Gets a packet to remove a special map object.
     *
     * @param summon
     * @param animated Animated removal?
     * @return The packet removing the object.
     */
    public static Packet removeSummon(Summon summon, bool animated)
    {
        OutPacket p = OutPacket.create(SendOpcode.REMOVE_SPECIAL_MAPOBJECT);
        p.writeInt(summon.getOwner().getId());
        p.writeInt(summon.getObjectId());
        p.writeByte(animated ? 4 : 1); // ?
        return p;
    }

    public static Packet spawnKite(int objId, int itemId, string name, string msg, Point pos, int ft)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_KITE);
        p.writeInt(objId);
        p.writeInt(itemId);
        p.writeString(msg);
        p.writeString(name);
        p.writeShort(pos.X);
        p.writeShort(ft);
        return p;
    }

    public static Packet removeKite(int objId, int animationType)
    {    // thanks to Arnah (Vertisy)
        OutPacket p = OutPacket.create(SendOpcode.REMOVE_KITE);
        p.writeByte(animationType); // 0 is 10/10, 1 just vanishes
        p.writeInt(objId);
        return p;
    }

    public static Packet sendCannotSpawnKite()
    {
        return OutPacket.create(SendOpcode.CANNOT_SPAWN_KITE);
    }

    /**
     * Gets the response to a relog request.
     *
     * @return The relog response packet.
     */
    public static Packet getRelogResponse()
    {
        OutPacket p = OutPacket.create(SendOpcode.RELOG_RESPONSE);
        p.writeByte(1);//1 O.O Must be more types ):
        return p;
    }

    /**
     * Gets a server message packet.
     *
     * @param message The message to convey.
     * @return The server message packet.
     */
    public static Packet serverMessage(string message)
    {
        return serverMessage(4, 0, message, true, false, 0);
    }

    /// <summary>
    /// Gets a server notice packet.
    /// </summary>
    /// <param name="type">The type of the notice.
    /// <para>Possible values for <paramref name="type"/>:</para>
    /// <para>0 - Notice</para>
    /// <para>1 - Popup</para>
    /// <para>2 - Megaphone</para>
    /// <para>3 - SuperMegaphone</para>
    /// <para>4 - ScrollingMessage at top</para>
    /// <para>5 - PinkText</para>
    /// <para>6 - Lightblue Text</para>
    /// </param>
    /// <param name="message">The message to convey.</param>
    /// <param name="npc"></param>
    /// <returns></returns>
    public static Packet serverNotice(int type, string message, int npc = 0)
    {
        return serverMessage(type, 0, message, false, false, npc);
    }

    public static Packet serverNotice(int type, int channel, string message, bool smegaEar = false)
    {
        return serverMessage(type, channel, message, false, smegaEar, 0);
    }

    /**
     * Gets a server message packet.
     * <p>
     * Possible values for <code>type</code>:<br> 0: [Notice]<br> 1: Popup<br>
     * 2: Megaphone<br> 3: Super Megaphone<br> 4: Scrolling message at top<br>
     * 5: Pink Text<br> 6: Lightblue Text<br> 7: BroadCasting NPC
     *
     * @param type          The type of the notice.
     * @param channel       The channel this notice was sent on.
     * @param message       The message to convey.
     * @param servermessage Is this a scrolling ticker?
     * @return The server notice packet.
     */
    private static Packet serverMessage(int type, int channel, string message, bool servermessage, bool megaEar, int npc)
    {
        OutPacket p = OutPacket.create(SendOpcode.SERVERMESSAGE);
        p.writeByte(type);
        if (servermessage)
        {
            p.writeByte(1);
        }
        p.writeString(message);
        if (type == 3)
        {
            p.writeByte(channel - 1); // channel
            p.writeBool(megaEar);
        }
        else if (type == 6)
        {
            p.writeInt(0);
        }
        else if (type == 7)
        { // npc
            p.writeInt(npc);
        }
        return p;
    }


    /// <summary>
    /// Sends a Avatar Super Megaphone packet.
    /// </summary>
    /// <param name="chr">The character name.</param>
    /// <param name="medal">The medal text.</param>
    /// <param name="channel">Which channel.</param>
    /// <param name="itemId">Which item used.</param>
    /// <param name="message">The message sent.</param>
    /// <param name="ear">Whether or not the ear is shown for whisper.</param>
    /// <returns></returns>
    public static Packet getAvatarMega(IPlayer chr, string medal, int channel, int itemId, List<string> message, bool ear)
    {
        OutPacket p = OutPacket.create(SendOpcode.SET_AVATAR_MEGAPHONE);
        p.writeInt(itemId);
        p.writeString(medal + chr.getName());
        foreach (string s in message)
        {
            p.writeString(s);
        }
        p.writeInt(channel - 1); // channel
        p.writeBool(ear);
        addCharLook(p, chr, true);
        return p;
    }


    /// <summary>
    /// Sends a packet to remove the tiger megaphone
    /// </summary>
    /// <returns></returns>
    public static Packet byeAvatarMega()
    {
        OutPacket p = OutPacket.create(SendOpcode.CLEAR_AVATAR_MEGAPHONE);
        p.writeByte(1);
        return p;
    }


    /// <summary>
    /// Sends the Gachapon green message when a user uses a gachapon ticket.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="town"></param>
    /// <param name="player"></param>
    /// <returns></returns>
    public static Packet gachaponMessage(Item item, string town, IPlayer player)
    {
        OutPacket p = OutPacket.create(SendOpcode.SERVERMESSAGE);
        p.writeByte(0x0B);
        p.writeString(player.getName() + " : got a(n)");
        p.writeInt(0); //random?
        p.writeString(town);
        addItemInfo(p, item, true);
        return p;
    }

    public static Packet spawnNPC(NPC life)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_NPC);
        p.writeInt(life.getObjectId());
        p.writeInt(life.getId());
        p.writeShort(life.getPosition().X);
        p.writeShort(life.getCy());
        p.writeBool(life.getF() != 1);
        p.writeShort(life.getFh());
        p.writeShort(life.getRx0());
        p.writeShort(life.getRx1());
        p.writeByte(1);
        return p;
    }

    public static Packet spawnNPCRequestController(NPC life, bool miniMap)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_NPC_REQUEST_CONTROLLER);
        p.writeByte(1);
        p.writeInt(life.getObjectId());
        p.writeInt(life.getId());
        p.writeShort(life.getPosition().X);
        p.writeShort(life.getCy());
        p.writeBool(life.getF() != 1);
        p.writeShort(life.getFh());
        p.writeShort(life.getRx0());
        p.writeShort(life.getRx1());
        p.writeBool(miniMap);
        return p;
    }


    /**
     * Gets a spawn monster packet.
     *
     * @param life     The monster to spawn.
     * @param newSpawn Is it a new spawn?
     * @param effect   The spawn effect.
     * @return The spawn monster packet.
     */
    public static Packet spawnMonster(Monster life, bool newSpawn, int effect = 0)
    {
        return spawnMonsterInternal(life, false, newSpawn, false, effect, false);
    }

    /**
     * Gets a control monster packet.
     *
     * @param life     The monster to give control to.
     * @param newSpawn Is it a new spawn?
     * @param aggro    Aggressive monster?
     * @return The monster control packet.
     */
    public static Packet controlMonster(Monster life, bool newSpawn, bool aggro)
    {
        return spawnMonsterInternal(life, true, newSpawn, aggro, 0, false);
    }

    /**
     * Removes a monster invisibility.
     *
     * @param life
     * @return
     */
    public static Packet removeMonsterInvisibility(Monster life)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_MONSTER_CONTROL);
        p.writeByte(1);
        p.writeInt(life.getObjectId());
        return p;
    }

    /**
     * Makes a monster invisible for Ariant PQ.
     *
     * @param life
     * @return
     */
    public static Packet makeMonsterInvisible(Monster life)
    {
        return spawnMonsterInternal(life, true, false, false, 0, true);
    }

    private static void encodeParentlessMobSpawnEffect(OutPacket p, bool newSpawn, int effect)
    {
        if (effect > 0)
        {
            p.writeByte(effect);
            p.writeByte(0);
            p.writeShort(0);
            if (effect == 15)
            {
                p.writeByte(0);
            }
        }
        p.writeByte(newSpawn ? -2 : -1);
    }

    private static void encodeTemporary(OutPacket p, Dictionary<MonsterStatus, MonsterStatusEffect> stati)
    {
        int pCounter = -1;
        int mCounter = -1;

        stati = stati  // to patch some status crashing players
                .Where(e => !(e.Key.Equals(MonsterStatus.WATK) || e.Key.Equals(MonsterStatus.WDEF)))
                .ToDictionary(x => x.Key, y => y.Value);

        writeLongEncodeTemporaryMask(p, stati.Keys);    // packet structure mapped thanks to Eric

        foreach (var s in stati)
        {
            MonsterStatusEffect mse = s.Value;
            p.writeShort(mse.getStati().GetValueOrDefault(s.Key));

            var mobSkill = mse.getMobSkill();
            if (mobSkill != null)
            {
                writeMobSkillId(p, mobSkill.getId());

                if (s.Key == MonsterStatus.WEAPON_REFLECT)
                    pCounter = mobSkill.getX();
                if (s.Key == MonsterStatus.MAGIC_REFLECT)
                    mCounter = mobSkill.getY();
            }
            else
            {
                var skill = mse.getSkill();
                p.writeInt(skill != null ? skill.getId() : 0);
            }

            p.writeShort(-1);    // duration
        }

        // reflect packet structure found thanks to Arnah (Vertisy)
        if (pCounter != -1)
        {
            p.writeInt(pCounter);// wPCounter_
        }
        if (mCounter != -1)
        {
            p.writeInt(mCounter);// wMCounter_
        }
        if (pCounter != -1 || mCounter != -1)
        {
            p.writeInt(100);// nCounterProb_
        }
    }

    /**
     * Internal function to handler monster spawning and controlling.
     *
     * @param life              The mob to perform operations with.
     * @param requestController Requesting control of mob?
     * @param newSpawn          New spawn (fade in?)
     * @param aggro             Aggressive mob?
     * @param effect            The spawn effect to use.
     * @return The spawn/control packet.
     */
    private static Packet spawnMonsterInternal(Monster life, bool requestController, bool newSpawn, bool aggro, int effect, bool makeInvis)
    {
        if (makeInvis)
        {
            var o = OutPacket.create(SendOpcode.SPAWN_MONSTER_CONTROL);
            o.writeByte(0);
            o.writeInt(life.getObjectId());
            return o;
        }

        OutPacket p;
        if (requestController)
        {
            p = OutPacket.create(SendOpcode.SPAWN_MONSTER_CONTROL);
            p.writeByte(aggro ? 2 : 1);
        }
        else
        {
            p = OutPacket.create(SendOpcode.SPAWN_MONSTER);
        }

        p.writeInt(life.getObjectId());
        p.writeByte(life.getController() == null ? 5 : 1);
        p.writeInt(life.getId());

        if (requestController)
        {
            encodeTemporary(p, life.getStati());    // thanks shot for noticing encode temporary buffs missing
        }
        else
        {
            p.skip(16);
        }

        p.writePos(life.getPosition());
        p.writeByte(life.getStance());
        p.writeShort(0); //Origin FH //life.getStartFh()
        p.writeShort(life.getFh());


        /**
         * -4: Fake -3: Appear after linked mob is dead -2: Fade in 1: Smoke 3:
         * King Slime spawn 4: Summoning rock thing, used for 3rd job? 6:
         * Magical shit 7: Smoke shit 8: 'The Boss' 9/10: Grim phantom shit?
         * 11/12: Nothing? 13: Frankenstein 14: Angry ^ 15: Orb animation thing,
         * ?? 16: ?? 19: Mushroom castle boss thing
         */

        if (life.getParentMobOid() != 0)
        {
            var parentMob = life.getMap().getMonsterByOid(life.getParentMobOid());
            if (parentMob != null && parentMob.isAlive())
            {
                p.writeByte(effect != 0 ? effect : -3);
                p.writeInt(life.getParentMobOid());
            }
            else
            {
                encodeParentlessMobSpawnEffect(p, newSpawn, effect);
            }
        }
        else
        {
            encodeParentlessMobSpawnEffect(p, newSpawn, effect);
        }

        p.writeByte(life.getTeam());
        p.writeInt(0); // getItemEffect
        return p;
    }

    /// <summary>
    /// Handles monsters not being targettable, such as Zakum's first body.
    /// </summary>
    /// <param name="life">The mob to spawn as non-targettable.</param>
    /// <param name="effect">The effect to show when spawning.</param>
    /// <returns>The packet to spawn the mob as non-targettable.</returns>
    public static Packet spawnFakeMonster(Monster life, int effect)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_MONSTER_CONTROL);
        p.writeByte(1);
        p.writeInt(life.getObjectId());
        p.writeByte(5);
        p.writeInt(life.getId());
        encodeTemporary(p, life.getStati());
        p.writePos(life.getPosition());
        p.writeByte(life.getStance());
        p.writeShort(0);//life.getStartFh()
        p.writeShort(life.getFh());
        if (effect > 0)
        {
            p.writeByte(effect);
            p.writeByte(0);
            p.writeShort(0);
        }
        p.writeShort(-2);
        p.writeByte(life.getTeam());
        p.writeInt(0);
        return p;
    }

    /// <summary>
    /// Makes a monster previously spawned as non-targettable, targettable.
    /// </summary>
    /// <param name="life">The mob to make targettable.</param>
    /// <returns>The packet to make the mob targettable.</returns>
    public static Packet makeMonsterReal(Monster life)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_MONSTER);
        p.writeInt(life.getObjectId());
        p.writeByte(5);
        p.writeInt(life.getId());
        encodeTemporary(p, life.getStati());
        p.writePos(life.getPosition());
        p.writeByte(life.getStance());
        p.writeShort(0);//life.getStartFh()
        p.writeShort(life.getFh());
        p.writeShort(-1);
        p.writeInt(0);
        return p;
    }

    /// <summary>
    /// Gets a stop control monster packet.
    /// </summary>
    /// <param name="oid">The ObjectID of the monster to stop controlling.</param>
    /// <returns>The stop control monster packet.</returns>
    public static Packet stopControllingMonster(int oid)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_MONSTER_CONTROL);
        p.writeByte(0);
        p.writeInt(oid);
        return p;
    }

    /// <summary>
    /// Gets a response to a move monster packet.
    /// </summary>
    /// <param name="objectid">The ObjectID of the monster being moved.</param>
    /// <param name="moveid">The movement ID.</param>
    /// <param name="currentMp">The current MP of the monster.</param>
    /// <param name="useSkills">Can the monster use skills?</param>
    /// <param name="skillId">The skill ID for the monster to use.</param>
    /// <param name="skillLevel">The level of the skill to use.</param>
    /// <returns></returns>

    public static Packet moveMonsterResponse(int objectid, short moveid, int currentMp, bool useSkills, int skillId = 0, int skillLevel = 0)
    {
        OutPacket p = OutPacket.create(SendOpcode.MOVE_MONSTER_RESPONSE);
        p.writeInt(objectid);
        p.writeShort(moveid);
        p.writeBool(useSkills);
        p.writeShort(currentMp);
        p.writeByte(skillId);
        p.writeByte(skillLevel);
        return p;
    }

    /// <summary>
    /// Gets a general chat packet.
    /// </summary>
    /// <param name="cidfrom">The character ID who sent the chat.</param>
    /// <param name="text">The text of the chat.</param>
    /// <param name="gm"></param>
    /// <param name="show"></param>
    /// <returns>The general chat packet.</returns>
    public static Packet getChatText(int cidfrom, string text, bool gm, int show)
    {
        OutPacket p = OutPacket.create(SendOpcode.CHATTEXT);
        p.writeInt(cidfrom);
        p.writeBool(gm);
        p.writeString(text);
        p.writeByte(show);
        return p;
    }

    /// <summary>
    /// Gets a packet telling the client to show an EXP increase.
    /// </summary>
    /// <param name="gain">The amount of EXP gained.</param>
    /// <param name="equip">In the chat box?</param>
    /// <param name="party">White text or yellow?</param>
    /// <param name="inChat"></param>
    /// <param name="white"></param>
    /// <returns>The exp gained packet.</returns>
    public static Packet getShowExpGain(int gain, int equip, int party, bool inChat, bool white)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        p.writeByte(3); // 3 = exp, 4 = fame, 5 = mesos, 6 = guildpoints
        p.writeBool(white);
        p.writeInt(gain);
        p.writeBool(inChat);
        p.writeInt(0); // bonus event exp
        p.writeByte(0); // third monster kill event
        p.writeByte(0); // RIP byte, this is always a 0
        p.writeInt(0); //wedding bonus
        if (inChat)
        { // quest bonus rate stuff
            p.writeByte(0);
        }

        p.writeByte(0); //0 = party bonus, 100 = 1x Bonus EXP, 200 = 2x Bonus EXP
        p.writeInt(party); // party bonus
        p.writeInt(equip); //equip bonus
        p.writeInt(0); //Internet Cafe Bonus
        p.writeInt(0); //Rainbow Week Bonus
        return p;
    }

    /// <summary>
    /// Gets a packet telling the client to show a fame gain.
    /// </summary>
    /// <param name="gain">How many fame gained.</param>
    /// <returns>The meso gain packet.</returns>
    public static Packet getShowFameGain(int gain)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        p.writeByte(4);
        p.writeInt(gain);
        return p;
    }


    /// <summary>
    /// Gets a packet telling the client to show a meso gain.
    /// </summary>
    /// <param name="gain">How many mesos gained.</param>
    /// <param name="inChat">Show in the chat window?</param>
    /// <returns>The meso gain packet.</returns>
    public static Packet getShowMesoGain(int gain, bool inChat = false)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        if (!inChat)
        {
            p.writeByte(0);
            p.writeShort(1); //v83
        }
        else
        {
            p.writeByte(5);
        }
        p.writeInt(gain);
        p.writeShort(0);
        return p;
    }

    /// <summary>
    /// Gets a packet telling the client to show a item gain.
    /// </summary>
    /// <param name="itemId">The ID of the item gained.</param>
    /// <param name="quantity">How many items gained.</param>
    /// <returns>The item gain packet.</returns>
    public static Packet getShowItemGain(int itemId, short quantity)
    {
        return getShowItemGain(itemId, quantity, false);
    }

    /// <summary>
    /// Gets a packet telling the client to show an item gain.
    /// </summary>
    /// <param name="itemId">The ID of the item gained.</param>
    /// <param name="quantity">The number of items gained.</param>
    /// <param name="inChat">Show in the chat window?</param>
    /// <returns>The item gain packet.</returns>
    public static Packet getShowItemGain(int itemId, short quantity, bool inChat)
    {
        OutPacket p;
        if (inChat)
        {
            p = OutPacket.create(SendOpcode.SHOW_ITEM_GAIN_INCHAT);
            p.writeByte(3);
            p.writeByte(1);
            p.writeInt(itemId);
            p.writeInt(quantity);
        }
        else
        {
            p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
            p.writeShort(0);
            p.writeInt(itemId);
            p.writeInt(quantity);
            p.writeInt(0);
            p.writeInt(0);
        }
        return p;
    }

    public static Packet killMonster(int objId, bool animation)
    {
        return killMonster(objId, animation ? 1 : 0);
    }

    /**
     * Gets a packet telling the client that a monster was killed.
     *
     * @param objId     The objectID of the killed monster.
     * @param animation 0 = dissapear, 1 = fade out, 2+ = special
     * @return The kill monster packet.
     */
    public static Packet killMonster(int objId, int animation)
    {
        OutPacket p = OutPacket.create(SendOpcode.KILL_MONSTER);
        p.writeInt(objId);
        p.writeByte(animation);
        p.writeByte(animation);
        return p;
    }

    public static Packet updateMapItemObject(MapItem drop, bool giveOwnership)
    {
        OutPacket p = OutPacket.create(SendOpcode.DROP_ITEM_FROM_MAPOBJECT);
        p.writeByte(2);
        p.writeInt(drop.getObjectId());
        p.writeBool(drop.getMeso() > 0);
        p.writeInt(drop.getItemId());
        p.writeInt(giveOwnership ? 0 : -1);
        p.writeByte(drop.hasExpiredOwnershipTime() ? 2 : drop.getDropType());
        p.writePos(drop.getPosition());
        p.writeInt(giveOwnership ? 0 : -1);

        if (drop.getMeso() == 0)
        {
            addExpirationTime(p, drop.getItem().getExpiration());
        }
        p.writeBool(!drop.isPlayerDrop());
        return p;
    }

    public static Packet dropItemFromMapObject(IPlayer player, MapItem drop, Point? dropfrom, Point dropto, byte mod, short delay)
    {
        int dropType = drop.getDropType();
        if (drop.hasClientsideOwnership(player) && dropType < 3)
        {
            dropType = 2;
        }

        OutPacket p = OutPacket.create(SendOpcode.DROP_ITEM_FROM_MAPOBJECT);
        p.writeByte(mod);
        p.writeInt(drop.getObjectId());
        p.writeBool(drop.getMeso() > 0); // 1 mesos, 0 item, 2 and above all item meso bag,
        p.writeInt(drop.getItemId()); // drop object ID
        p.writeInt(drop.getClientsideOwnerId()); // owner charid/partyid :)
        p.writeByte(dropType); // 0 = timeout for non-owner, 1 = timeout for non-owner's party, 2 = FFA, 3 = explosive/FFA
        p.writePos(dropto);
        p.writeInt(drop.getDropper().getObjectId()); // dropper oid, found thanks to Li Jixue

        if (mod != 2)
        {
            p.writePos(dropfrom!.Value);
            p.writeShort(delay);//Fh?
        }
        if (drop.getMeso() == 0)
        {
            addExpirationTime(p, drop.getItem().getExpiration());
        }
        p.writeByte(drop.isPlayerDrop() ? 0 : 1); //pet EQP pickup
        return p;
    }

    private static void writeForeignBuffs(OutPacket p, IPlayer chr)
    {
        p.writeInt(0);
        p.writeShort(0); //v83
        p.writeByte(0xFC);
        p.writeByte(1);
        if (chr.getBuffedValue(BuffStat.MORPH) != null)
        {
            p.writeInt(2);
        }
        else
        {
            p.writeInt(0);
        }
        long buffmask = 0;
        int? buffvalue = null;
        if ((chr.getBuffedValue(BuffStat.DARKSIGHT) != null || chr.getBuffedValue(BuffStat.WIND_WALK) != null) && !chr.isHidden())
        {
            buffmask |= BuffStat.DARKSIGHT.getValue();
        }
        if (chr.getBuffedValue(BuffStat.COMBO) != null)
        {
            buffmask |= BuffStat.COMBO.getValue();
            buffvalue = chr.getBuffedValue(BuffStat.COMBO);
        }
        if (chr.getBuffedValue(BuffStat.SHADOWPARTNER) != null)
        {
            buffmask |= BuffStat.SHADOWPARTNER.getValue();
        }
        if (chr.getBuffedValue(BuffStat.SOULARROW) != null)
        {
            buffmask |= BuffStat.SOULARROW.getValue();
        }
        if (chr.getBuffedValue(BuffStat.MORPH) != null)
        {
            buffvalue = chr.getBuffedValue(BuffStat.MORPH);
        }
        p.writeInt((int)((buffmask >> 32) & 0xffffffffL));
        if (buffvalue != null)
        {
            if (chr.getBuffedValue(BuffStat.MORPH) != null)
            { //TEST
                p.writeShort(buffvalue.Value);
            }
            else
            {
                p.writeByte(buffvalue.Value);
            }
        }
        p.writeInt((int)(buffmask & 0xffffffffL));

        // Energy Charge
        p.writeInt(chr.getEnergyBar() == 15000 ? 1 : 0);
        p.writeShort(0);
        p.skip(4);

        bool dashBuff = chr.getBuffedValue(BuffStat.DASH) != null;
        // Dash Speed
        p.writeInt(dashBuff ? 1 << 24 : 0);
        p.skip(11);
        p.writeShort(0);
        // Dash Jump
        p.skip(9);
        p.writeInt(dashBuff ? 1 << 24 : 0);
        p.writeShort(0);
        p.writeByte(0);

        // NewMonster Riding
        int? bv = chr.getBuffedValue(BuffStat.MONSTER_RIDING);
        if (bv != null)
        {
            var mount = chr.getMount();
            if (mount != null)
            {
                p.writeInt(mount.getItemId());
                p.writeInt(mount.getSkillId());
            }
            else
            {
                p.writeLong(0);
            }
        }
        else
        {
            p.writeLong(0);
        }

        int CHAR_MAGIC_SPAWN = Randomizer.nextInt();    // skill references found thanks to Rien dev team
        p.writeInt(CHAR_MAGIC_SPAWN);
        // Speed Infusion
        p.skip(8);
        p.writeInt(CHAR_MAGIC_SPAWN);
        p.writeByte(0);
        p.writeInt(CHAR_MAGIC_SPAWN);
        p.writeShort(0);
        // Homing Beacon
        p.skip(9);
        p.writeInt(CHAR_MAGIC_SPAWN);
        p.writeInt(0);
        // Zombify
        p.skip(9);
        p.writeInt(CHAR_MAGIC_SPAWN);
        p.writeShort(0);
        p.writeShort(0);
    }

    /**
     * Gets a packet spawning a player as a mapobject to other clients.
     *
     * @param target        The client receiving this packet.
     * @param chr           The character to spawn to other clients.
     * @param enteringField Whether the character to spawn is not yet present in the map or already is.
     * @return The spawn player packet.
     */
    public static Packet spawnPlayerMapObject(IClient target, IPlayer chr, bool enteringField)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_PLAYER);
        p.writeInt(chr.getId());
        p.writeByte(chr.getLevel()); //v83
        p.writeString(chr.getName());
        if (chr.getGuildId() < 1)
        {
            p.writeString("");
            p.writeBytes(new byte[6]);
        }
        else
        {
            var gs = chr.GuildModel;
            if (gs != null)
            {
                p.writeString(gs.Name);
                p.writeShort(gs.LogoBg);
                p.writeByte(gs.LogoBgColor);
                p.writeShort(gs.Logo);
                p.writeByte(gs.LogoColor);
            }
            else
            {
                p.writeString("");
                p.writeBytes(new byte[6]);
            }
        }

        writeForeignBuffs(p, chr);

        p.writeShort(chr.getJob().getId());

        /* replace "p.writeShort(chr.getJob().getId())" with this snippet for 3rd person FJ animation on all classes
        if (chr.getJob().isA(Job.HERMIT) || chr.getJob().isA(Job.DAWNWARRIOR2) || chr.getJob().isA(Job.NIGHTWALKER2)) {
    p.writeShort(chr.getJob().getId());
        } else {
    p.writeShort(412);
        }*/

        addCharLook(p, chr, false);
        p.writeInt(chr.getInventory(InventoryType.CASH).countById(ItemId.HEART_SHAPED_CHOCOLATE));
        p.writeInt(chr.getItemEffect());
        p.writeInt(ItemConstants.getInventoryType(chr.getChair()) == InventoryType.SETUP ? chr.getChair() : 0);

        if (enteringField)
        {
            Point spawnPos = chr.getPosition();
            spawnPos.Y -= 42;
            p.writePos(spawnPos);
            p.writeByte(6);
        }
        else
        {
            p.writePos(chr.getPosition());
            p.writeByte(chr.getStance());
        }

        p.writeShort(0);//chr.getFh()
        p.writeByte(0);
        Pet?[] pet = chr.getPets();
        for (int i = 0; i < 3; i++)
        {
            if (pet[i] != null)
            {
                addPetInfo(p, pet[i]!, false);
            }
        }
        p.writeByte(0); //end of pets

        var chrMount = chr.getMount();
        if (chrMount == null)
        {
            p.writeInt(1); // mob level
            p.writeLong(0); // mob exp + tiredness
        }
        else
        {
            p.writeInt(chrMount.getLevel());
            p.writeInt(chrMount.getExp());
            p.writeInt(chrMount.getTiredness());
        }

        var mps = chr.getPlayerShop();
        if (mps != null && mps.isOwner(chr))
        {
            if (mps.hasFreeSlot())
            {
                addAnnounceBox(p, mps, mps.getVisitors().Length);
            }
            else
            {
                addAnnounceBox(p, mps, 1);
            }
        }
        else
        {
            var miniGame = chr.getMiniGame();
            if (miniGame != null && miniGame.isOwner(chr))
            {
                if (miniGame.hasFreeSlot())
                {
                    addAnnounceBox(p, miniGame, 1, 0);
                }
                else
                {
                    addAnnounceBox(p, miniGame, 2, miniGame.isMatchInProgress() ? 1 : 0);
                }
            }
            else
            {
                p.writeByte(0);
            }
        }

        if (chr.getChalkboard() != null)
        {
            p.writeByte(1);
            p.writeString(chr.getChalkboard()!);
        }
        else
        {
            p.writeByte(0);
        }
        addRingLook(p, chr, true);  // crush
        addRingLook(p, chr, false); // friendship
        addMarriageRingLook(target, p, chr);
        encodeNewYearCardInfo(p, chr);  // new year seems to crash sometimes...
        p.writeByte(0);
        p.writeByte(0);
        p.writeByte(chr.getTeam());//only needed in specific fields
        return p;
    }

    private static void encodeNewYearCardInfo(OutPacket p, IPlayer chr)
    {
        HashSet<NewYearCardRecord> newyears = chr.getReceivedNewYearRecords();
        if (newyears.Count > 0)
        {
            p.writeByte(1);

            p.writeInt(newyears.Count);
            foreach (NewYearCardRecord nyc in newyears)
            {
                p.writeInt(nyc.getId());
            }
        }
        else
        {
            p.writeByte(0);
        }
    }

    public static Packet onNewYearCardRes(IPlayer user, int cardId, int mode, int msg)
    {
        var newyear = user.getNewYearRecord(cardId);
        return onNewYearCardRes(user, newyear, mode, msg);
    }

    public static Packet onNewYearCardRes(IPlayer user, NewYearCardRecord? newyear, int mode, int msg)
    {
        OutPacket p = OutPacket.create(SendOpcode.NEW_YEAR_CARD_RES);
        p.writeByte(mode);
        switch (mode)
        {
            case 4: // Successfully sent a New Year Card\r\n to %s.
            case 6: // Successfully received a New Year Card.
                encodeNewYearCard(newyear!, p);
                break;

            case 8: // Successfully deleted a New Year Card.
                p.writeInt(newyear!.getId());
                break;

            case 5: // Nexon's stupid and makes 4 modes do the same operation..
            case 7:
            case 9:
            case 0xB:
                // 0x10: You have no free slot to store card.\r\ntry later on please.
                // 0x11: You have no card to send.
                // 0x12: Wrong inventory information !
                // 0x13: Cannot find such character !
                // 0x14: Incoherent Data !
                // 0x15: An error occured during DB operation.
                // 0x16: An unknown error occured !
                // 0xF: You cannot send a card to yourself !
                p.writeByte(msg);
                break;

            case 0xA:   // GetUnreceivedList_Done
                int nSN = 1;
                p.writeInt(nSN);
                if ((nSN - 1) <= 98 && nSN > 0)
                {
                    //lol nexon are you kidding
                    for (int i = 0; i < nSN; i++)
                    {
                        p.writeInt(newyear!.getId());
                        p.writeInt(newyear.getSenderId());
                        p.writeString(newyear.getSenderName());
                    }
                }
                break;

            case 0xC:   // NotiArrived
                p.writeInt(newyear!.getId());
                p.writeString(newyear.getSenderName());
                break;

            case 0xD:   // BroadCast_AddCardInfo
                p.writeInt(newyear!.getId());
                p.writeInt(user.getId());
                break;

            case 0xE:   // BroadCast_RemoveCardInfo
                p.writeInt(newyear!.getId());
                break;
        }
        return p;
    }

    private static void encodeNewYearCard(NewYearCardRecord newyear, OutPacket p)
    {
        p.writeInt(newyear.getId());
        p.writeInt(newyear.getSenderId());
        p.writeString(newyear.getSenderName());
        p.writeBool(newyear.isSenderCardDiscarded());
        p.writeLong(newyear.getDateSent());
        p.writeInt(newyear.getReceiverId());
        p.writeString(newyear.getReceiverName());
        p.writeBool(newyear.isReceiverCardDiscarded());
        p.writeBool(newyear.isReceiverCardReceived());
        p.writeLong(newyear.getDateReceived());
        p.writeString(newyear.getMessage());
    }

    private static void addRingLook(OutPacket p, IPlayer chr, bool crush)
    {
        List<Ring> rings;
        if (crush)
        {
            rings = chr.getCrushRings();
        }
        else
        {
            rings = chr.getFriendshipRings();
        }
        bool yes = false;
        foreach (Ring ring in rings)
        {
            if (ring.equipped())
            {
                if (!yes)
                {
                    yes = true;
                    p.writeByte(1);
                }
                p.writeInt(ring.getRingId());
                p.writeInt(0);
                p.writeInt(ring.getPartnerRingId());
                p.writeInt(0);
                p.writeInt(ring.getItemId());
            }
        }
        if (!yes)
        {
            p.writeByte(0);
        }
    }

    private static void addMarriageRingLook(IClient target, OutPacket p, IPlayer chr)
    {
        var ring = chr.getMarriageRing();

        if (ring == null || !ring.equipped())
        {
            p.writeByte(0);
        }
        else
        {
            p.writeByte(1);

            var targetChr = target.getPlayer();
            if (targetChr != null && targetChr.getPartnerId() == chr.getId())
            {
                p.writeInt(0);
                p.writeInt(0);
            }
            else
            {
                p.writeInt(chr.getId());
                p.writeInt(ring.getPartnerChrId());
            }

            p.writeInt(ring.getItemId());
        }
    }

    /**
     * Adds an announcement box to an existing OutPacket.
     *
     * @param p    The OutPacket to add an announcement box
     *             to.
     * @param shop The shop to announce.
     */
    private static void addAnnounceBox(OutPacket p, PlayerShop shop, int availability)
    {
        p.writeByte(4);
        p.writeInt(shop.getObjectId());
        p.writeString(shop.getDescription());
        p.writeByte(0);
        p.writeByte(0);
        p.writeByte(1);
        p.writeByte(availability);
        p.writeByte(0);
    }

    private static void addAnnounceBox(OutPacket p, MiniGame game, int ammount, int joinable)
    {
        p.writeByte((byte)game.getGameType());
        p.writeInt(game.getObjectId()); // gameid/shopid
        p.writeString(game.getDescription()); // desc
        p.writeBool(game.getPassword().Count() > 0);    // password here, thanks GabrielSin
        p.writeByte(game.getPieceType());
        p.writeByte(ammount);
        p.writeByte(2);         //player capacity
        p.writeByte(joinable);
    }

    private static void updateHiredMerchantBoxInfo(OutPacket p, HiredMerchant hm)
    {
        byte[] roomInfo = hm.getShopRoomInfo();

        p.writeByte(5);
        p.writeInt(hm.getObjectId());
        p.writeString(hm.getDescription());
        p.writeByte(hm.getItemId() % 100);
        p.writeBytes(roomInfo);    // visitor capacity here, thanks GabrielSin
    }

    public static Packet updateHiredMerchantBox(HiredMerchant hm)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_HIRED_MERCHANT);
        p.writeInt(hm.getOwnerId());
        updateHiredMerchantBoxInfo(p, hm);
        return p;
    }

    private static void updatePlayerShopBoxInfo(OutPacket p, PlayerShop shop)
    {
        byte[] roomInfo = shop.getShopRoomInfo();

        p.writeByte(4);
        p.writeInt(shop.getObjectId());
        p.writeString(shop.getDescription());
        p.writeByte(0);                 // pw
        p.writeByte(shop.getItemId() % 100);
        p.writeByte(roomInfo[0]);       // curPlayers
        p.writeByte(roomInfo[1]);       // maxPlayers
        p.writeByte(0);
    }

    public static Packet updatePlayerShopBox(PlayerShop shop)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_CHAR_BOX);
        p.writeInt(shop.getOwner().getId());
        updatePlayerShopBoxInfo(p, shop);
        return p;
    }

    public static Packet removePlayerShopBox(PlayerShop shop)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_CHAR_BOX);
        p.writeInt(shop.getOwner().getId());
        p.writeByte(0);
        return p;
    }

    public static Packet facialExpression(IPlayer from, int expression)
    {
        OutPacket p = OutPacket.create(SendOpcode.FACIAL_EXPRESSION);
        p.writeInt(from.getId());
        p.writeInt(expression);
        return p;
    }

    private static void rebroadcastMovementList(OutPacket op, InPacket ip, long movementDataLength)
    {
        //movement command length is sent by client, probably not a big issue? (could be calculated on server)
        //if multiple write/reads are slow, could use (and cache?) a byte[] buffer
        for (long i = 0; i < movementDataLength; i++)
        {
            op.writeByte(ip.readByte());
        }
    }

    private static void serializeMovementList(OutPacket p, List<LifeMovementFragment> moves)
    {
        p.writeByte(moves.Count);
        foreach (LifeMovementFragment move in moves)
        {
            move.serialize(p);
        }
    }

    public static Packet movePlayer(int chrId, InPacket movementPacket, long movementDataLength)
    {
        OutPacket p = OutPacket.create(SendOpcode.MOVE_PLAYER);
        p.writeInt(chrId);
        p.writeInt(0);
        rebroadcastMovementList(p, movementPacket, movementDataLength);
        return p;
    }

    public static Packet moveSummon(int cid, int oid, Point startPos, InPacket movementPacket, long movementDataLength)
    {
        OutPacket p = OutPacket.create(SendOpcode.MOVE_SUMMON);
        p.writeInt(cid);
        p.writeInt(oid);
        p.writePos(startPos);
        rebroadcastMovementList(p, movementPacket, movementDataLength);
        return p;
    }

    public static Packet moveMonster(int oid, bool skillPossible, int skill, int skillId, int skillLevel, int pOption,
                                     Point startPos, InPacket movementPacket, long movementDataLength)
    {
        OutPacket p = OutPacket.create(SendOpcode.MOVE_MONSTER);
        p.writeInt(oid);
        p.writeByte(0);
        p.writeBool(skillPossible);
        p.writeByte(skill);
        p.writeByte(skillId);
        p.writeByte(skillLevel);
        p.writeShort(pOption);
        p.writePos(startPos);
        rebroadcastMovementList(p, movementPacket, movementDataLength);
        return p;
    }

    public static Packet summonAttack(int cid, int summonOid, byte direction, List<SummonAttackEntry> allDamage)
    {
        OutPacket p = OutPacket.create(SendOpcode.SUMMON_ATTACK);
        //b2 00 29 f7 00 00 9a a3 04 00 c8 04 01 94 a3 04 00 06 ff 2b 00
        p.writeInt(cid);
        p.writeInt(summonOid);
        p.writeByte(0);     // char level
        p.writeByte(direction);
        p.writeByte(allDamage.Count);
        foreach (SummonAttackEntry attackEntry in allDamage)
        {
            p.writeInt(attackEntry.monsterOid); // oid
            p.writeByte(6); // who knows
            p.writeInt(attackEntry.damage); // damage
        }

        return p;
    }

    /*
    public static Packet summonAttack(int cid, int summonSkillId, byte direction, List<SummonAttackEntry> allDamage) {
            OutPacket p = OutPacket.create(SendOpcode);
            //b2 00 29 f7 00 00 9a a3 04 00 c8 04 01 94 a3 04 00 06 ff 2b 00
            SUMMON_ATTACK);
            p.writeInt(cid);
            p.writeInt(summonSkillId);
            p.writeByte(direction);
            p.writeByte(4);
            p.writeByte(allDamage.Count);
            foreach(SummonAttackEntry attackEntry in allDamage) {
                    p.writeInt(attackEntry.getMonsterOid()); // oid
                    p.writeByte(6); // who knows
                    p.writeInt(attackEntry.getDamage()); // damage
            }
            return p;
    }
    */

    public static Packet closeRangeAttack(IPlayer chr, int skill, int skilllevel, int stance, int numAttackedAndDamage, Dictionary<int, AttackTarget?> damage, int speed, int direction, int display)
    {
        OutPacket p = OutPacket.create(SendOpcode.CLOSE_RANGE_ATTACK);
        addAttackBody(p, chr, skill, skilllevel, stance, numAttackedAndDamage, 0, damage, speed, direction, display);
        return p;
    }

    public static Packet rangedAttack(IPlayer chr, int skill, int skilllevel, int stance, int numAttackedAndDamage, int projectile, Dictionary<int, AttackTarget?> damage, int speed, int direction, int display)
    {
        OutPacket p = OutPacket.create(SendOpcode.RANGED_ATTACK);
        addAttackBody(p, chr, skill, skilllevel, stance, numAttackedAndDamage, projectile, damage, speed, direction, display);
        p.writeInt(0);
        return p;
    }

    public static Packet magicAttack(IPlayer chr, int skill, int skilllevel, int stance, int numAttackedAndDamage, Dictionary<int, AttackTarget?> damage, int charge, int speed, int direction, int display)
    {
        OutPacket p = OutPacket.create(SendOpcode.MAGIC_ATTACK);
        addAttackBody(p, chr, skill, skilllevel, stance, numAttackedAndDamage, 0, damage, speed, direction, display);
        if (charge != -1)
        {
            p.writeInt(charge);
        }
        return p;
    }

    private static void addAttackBody(OutPacket p, IPlayer chr, int skill, int skilllevel, int stance, int numAttackedAndDamage, int projectile, Dictionary<int, AttackTarget?> damage, int speed, int direction, int display)
    {
        p.writeInt(chr.getId());
        p.writeByte(numAttackedAndDamage);
        p.writeByte(0x5B);//?
        p.writeByte(skilllevel);
        if (skilllevel > 0)
        {
            p.writeInt(skill);
        }
        p.writeByte(display);
        p.writeByte(direction);
        p.writeByte(stance);
        p.writeByte(speed);
        p.writeByte(0x0A);
        p.writeInt(projectile);
        foreach (var oned in damage)
        {
            var onedList = oned.Value;
            if (onedList != null)
            {
                p.writeInt(oned.Key);
                p.writeByte(0x0);
                if (skill == ChiefBandit.MESO_EXPLOSION)
                {
                    p.writeByte(onedList.damageLines.Count);
                }
                foreach (int eachd in onedList.damageLines)
                {
                    p.writeInt(eachd);
                }
            }
        }
    }

    public static Packet throwGrenade(int cid, Point pos, int keyDown, int skillId, int skillLevel)
    { // packets found thanks to GabrielSin
        OutPacket p = OutPacket.create(SendOpcode.THROW_GRENADE);
        p.writeInt(cid);
        p.writeInt(pos.X);
        p.writeInt(pos.Y);
        p.writeInt(keyDown);
        p.writeInt(skillId);
        p.writeInt(skillLevel);
        return p;
    }

    // someone thought it was a good idea to handle floating point representation through packets ROFL
    private static int doubleToShortBits(double d)
    {
        return (short)(BitConverter.DoubleToInt64Bits(d) >> 48);
    }

    public static Packet getNPCShop(IClient c, int sid, List<ShopItem> items)
    {
        ItemInformationProvider ii = ItemInformationProvider.getInstance();
        OutPacket p = OutPacket.create(SendOpcode.OPEN_NPC_SHOP);
        p.writeInt(sid);
        p.writeShort(items.Count); // item count
        foreach (ShopItem item in items)
        {
            p.writeInt(item.getItemId());
            p.writeInt(item.getPrice());
            p.writeInt(item.getPrice() == 0 ? item.getPitch() : 0); //Perfect Pitch
            p.writeInt(0); //Can be used x minutes after purchase
            p.writeInt(0); //Hmm
            if (!ItemConstants.isRechargeable(item.getItemId()))
            {
                p.writeShort(1); // stacksize o.o
                p.writeShort(item.getBuyable());
            }
            else
            {
                p.writeShort(0);
                p.writeInt(0);
                p.writeShort(doubleToShortBits(ii.getUnitPrice(item.getItemId())));
                p.writeShort(ii.getSlotMax(c, item.getItemId()));
            }
        }
        return p;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="code">
    /// <para>00 = /</para>
    /// <para>01 = You don't have enough in stock</para>
    /// <para>02 = You do not have enough mesos</para>
    /// <para>03 = Please check if your inventory is full or not</para>
    /// <para>05 = You don't have enough in stock</para>
    /// <para>06 = Due to an error, the trade did not happen</para>
    /// <para>07 = Due to an error, the trade did not happen</para>
    /// <para>08 = /</para>
    /// <para>0D = You need more items</para>
    /// <para>0E = CRASH; LENGTH NEEDS TO BE LONGER :O</para>
    /// </param>
    /// <returns></returns>
    public static Packet shopTransaction(byte code)
    {
        OutPacket p = OutPacket.create(SendOpcode.CONFIRM_SHOP_TRANSACTION);
        p.writeByte(code);
        return p;
    }

    public static Packet updateInventorySlotLimit(int type, int newLimit)
    {
        OutPacket p = OutPacket.create(SendOpcode.INVENTORY_GROW);
        p.writeByte(type);
        p.writeByte(newLimit);
        return p;
    }

    public static Packet modifyInventory(bool updateTick, List<ModifyInventory> mods)
    {
        OutPacket p = OutPacket.create(SendOpcode.INVENTORY_OPERATION);
        p.writeBool(updateTick);
        p.writeByte(mods.Count);
        //p.writeByte(0); v104 :)
        int addMovement = -1;
        foreach (ModifyInventory mod in mods)
        {
            p.writeByte(mod.getMode());
            p.writeByte(mod.getInventoryType());
            p.writeShort(mod.getMode() == 2 ? mod.getOldPosition() : mod.getPosition());
            switch (mod.getMode())
            {
                case 0:
                    {//add item
                        addItemInfo(p, mod.getItem(), true);
                        break;
                    }
                case 1:
                    {//update quantity
                        p.writeShort(mod.getQuantity());
                        break;
                    }
                case 2:
                    {//move
                        p.writeShort(mod.getPosition());
                        if (mod.getPosition() < 0 || mod.getOldPosition() < 0)
                        {
                            addMovement = mod.getOldPosition() < 0 ? 1 : 2;
                        }
                        break;
                    }
                case 3:
                    {//remove
                        if (mod.getPosition() < 0)
                        {
                            addMovement = 2;
                        }
                        break;
                    }
            }
            mod.clear();
        }
        if (addMovement > -1)
        {
            p.writeByte(addMovement);
        }
        return p;
    }

    public static Packet getScrollEffect(int chr, ScrollResult scrollSuccess, bool legendarySpirit, bool whiteScroll)
    {   
        // thanks to Rien dev team
        OutPacket p = OutPacket.create(SendOpcode.SHOW_SCROLL_EFFECT);
        p.writeInt(chr);
        p.writeBool(scrollSuccess == ScrollResult.SUCCESS);
        p.writeBool(scrollSuccess == ScrollResult.CURSE);
        p.writeBool(legendarySpirit);
        p.writeBool(whiteScroll);
        return p;
    }

    public static Packet removePlayerFromMap(int chrId)
    {
        OutPacket p = OutPacket.create(SendOpcode.REMOVE_PLAYER_FROM_MAP);
        p.writeInt(chrId);
        return p;
    }

    public static Packet catchMessage(int message)
    { 
        // not done, I guess
        OutPacket p = OutPacket.create(SendOpcode.BRIDLE_MOB_CATCH_FAIL);
        p.writeByte(message); // 1 = too strong, 2 = Elemental Rock
        p.writeInt(0);//Maybe itemid?
        p.writeInt(0);
        return p;
    }

    public static Packet showAllCharacter(int totalWorlds, int totalChrs)
    {
        OutPacket p = OutPacket.create(SendOpcode.VIEW_ALL_CHAR);
        p.writeByte(totalChrs > 0 ? 1 : 5); // 2: already connected to server, 3 : unk error (view-all-characters), 5 : cannot find any
        p.writeInt(totalWorlds);
        p.writeInt(totalChrs);
        return p;
    }

    public static Packet showAriantScoreBoard()
    {   
        // thanks lrenex for pointing match's end scoreboard packet
        return OutPacket.create(SendOpcode.ARIANT_ARENA_SHOW_RESULT);
    }

    public static Packet updateAriantPQRanking(IPlayer chr, int score)
    {
        return updateAriantPQRanking(
            new Dictionary<IPlayer, int>() {
                { chr, score  }
            });
    }

    public static Packet updateAriantPQRanking(Dictionary<IPlayer, int> playerScore)
    {
        OutPacket p = OutPacket.create(SendOpcode.ARIANT_ARENA_USER_SCORE);
        p.writeByte(playerScore.Count);
        foreach (var e in playerScore)
        {
            p.writeString(e.Key.getName());
            p.writeInt(e.Value);
        }
        return p;
    }

    public static Packet updateWitchTowerScore(int score)
    {
        OutPacket p = OutPacket.create(SendOpcode.WITCH_TOWER_SCORE_UPDATE);
        p.writeByte(score);
        return p;
    }

    public static Packet silentRemoveItemFromMap(int objId)
    {
        return removeItemFromMap(objId, 1, 0);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="objId"></param>
    /// <param name="animation">
    /// <para>0 - expire</para>
    /// <para>1 - without animation</para>
    /// <para>2 - pickup</para>
    /// <para>4 - explode</para>
    /// <para>cid is ignored for 0 and 1.</para>
    /// </param>
    /// <param name="chrId"></param>
    /// <param name="pet">true will make a pet pick up the item.</param>
    /// <param name="slot"></param>
    /// <returns></returns>
    public static Packet removeItemFromMap(int objId, int animation, int chrId, bool pet = false, int slot = 0)
    {
        OutPacket p = OutPacket.create(SendOpcode.REMOVE_ITEM_FROM_MAP);
        p.writeByte(animation); // expire
        p.writeInt(objId);
        if (animation >= 2)
        {
            p.writeInt(chrId);
            if (pet)
            {
                p.writeByte(slot);
            }
        }
        return p;
    }
    public static Packet removeExplodedMesoFromMap(int mapObjectId, short delay)
    {
        OutPacket p = OutPacket.create(SendOpcode.REMOVE_ITEM_FROM_MAP);
        p.writeByte(4);
        p.writeInt(mapObjectId);
        p.writeShort(delay);
        return p;
    }

    public static Packet updateCharLook(IClient target, IPlayer chr)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_CHAR_LOOK);
        p.writeInt(chr.getId());
        p.writeByte(1);
        addCharLook(p, chr, false);
        addRingLook(p, chr, true);
        addRingLook(p, chr, false);
        addMarriageRingLook(target, p, chr);
        p.writeInt(0);
        return p;
    }

    public static Packet damagePlayer(int skill, int monsteridfrom, int cid, int damage, int fake, int direction, bool pgmr, int pgmr_1, bool is_pg, int oid, int pos_x, int pos_y)
    {
        OutPacket p = OutPacket.create(SendOpcode.DAMAGE_PLAYER);
        p.writeInt(cid);
        p.writeByte(skill);
        if (skill == -3)
        {
            p.writeInt(0);
        }
        p.writeInt(damage);
        if (skill != -4)
        {
            p.writeInt(monsteridfrom);
            p.writeByte(direction);
            if (pgmr)
            {
                p.writeByte(pgmr_1);
                p.writeByte(is_pg ? 1 : 0);
                p.writeInt(oid);
                p.writeByte(6);
                p.writeShort(pos_x);
                p.writeShort(pos_y);
                p.writeByte(0);
            }
            else
            {
                p.writeShort(0);
            }
            p.writeInt(damage);
            if (fake > 0)
            {
                p.writeInt(fake);
            }
        }
        else
        {
            p.writeInt(damage);
        }

        return p;
    }

    public static Packet sendMapleLifeCharacterInfo()
    {
        OutPacket p = OutPacket.create(SendOpcode.MAPLELIFE_RESULT);
        p.writeInt(0);
        return p;
    }

    public static Packet sendMapleLifeNameError()
    {
        OutPacket p = OutPacket.create(SendOpcode.MAPLELIFE_RESULT);
        p.writeInt(2);
        p.writeInt(3);
        p.writeByte(0);
        return p;
    }

    public static Packet sendMapleLifeError(int code)
    {
        OutPacket p = OutPacket.create(SendOpcode.MAPLELIFE_ERROR);
        p.writeByte(0);
        p.writeInt(code);
        return p;
    }

    public static Packet charNameResponse(string charname, bool nameUsed)
    {
        OutPacket p = OutPacket.create(SendOpcode.CHAR_NAME_RESPONSE);
        p.writeString(charname);
        p.writeByte(nameUsed ? 1 : 0);
        return p;
    }

    public static Packet addNewCharEntry(IPlayer chr)
    {
        OutPacket p = OutPacket.create(SendOpcode.ADD_NEW_CHAR_ENTRY);
        p.writeByte(0);
        addCharEntry(p, chr, false);
        return p;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cid"></param>
    /// <param name="state">
    /// <para>0x00 = success</para>
    /// <para>0x06 = Trouble logging into the game?</para>
    /// <para>0x09 = Unknown error</para>
    /// <para>0x0A = Could not be processed due to too many connection requests to the server.</para>
    /// <para>0x12 = invalid bday</para>
    /// <para>0x14 = incorrect pic</para>
    /// <para>0x16 = Cannot delete a guild master.</para>
    /// <para>0x18 = Cannot delete a character with a pending wedding.</para>
    /// <para>0x1A = Cannot delete a character with a pending world transfer.</para>
    /// <para>0x1D = Cannot delete a character that has a family.</para>
    /// </param>
    /// <returns></returns>
    public static Packet deleteCharResponse(int cid, int state)
    {
        OutPacket p = OutPacket.create(SendOpcode.DELETE_CHAR_RESPONSE);
        p.writeInt(cid);
        p.writeByte(state);
        return p;
    }

    public static Packet selectWorld(int world)
    {
        OutPacket p = OutPacket.create(SendOpcode.LAST_CONNECTED_WORLD);
        p.writeInt(world);//According to GMS, it should be the world that contains the most characters (most active)
        return p;
    }

    public static Packet sendRecommended(List<KeyValuePair<int, string>> worlds)
    {
        OutPacket p = OutPacket.create(SendOpcode.RECOMMENDED_WORLD_MESSAGE);
        p.writeByte(worlds.Count);//size
        foreach (var world in worlds)
        {
            p.writeInt(world.Key);
            p.writeString(world.Value);
        }
        return p;
    }

    /**
     * @param chr
     * @param isSelf
     * @return
     */
    public static Packet charInfo(IPlayer chr)
    {
        //3D 00 0A 43 01 00 02 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
        OutPacket p = OutPacket.create(SendOpcode.CHAR_INFO);
        p.writeInt(chr.getId());
        p.writeByte(chr.getLevel());
        p.writeShort(chr.getJob().getId());
        p.writeShort(chr.getFame());
        p.writeByte(chr.getMarriageRing() != null ? 1 : 0);
        string guildName = "";
        string allianceName = "";
        if (chr.getGuildId() > 0)
        {
            guildName = chr.GuildModel?.Name ?? "";

            if (chr.AllianceModel != null)
            {
                allianceName = chr.AllianceModel.getName();
            }
        }
        p.writeString(guildName);
        p.writeString(allianceName);  // does not seem to work
        p.writeByte(0); // pMedalInfo, thanks to Arnah (Vertisy)

        var pets = chr.getPets();
        var inv = chr.getInventory(InventoryType.EQUIPPED).getItem(-114);
        for (int i = 0; i < 3; i++)
        {
            var petObj = pets[i];
            if (petObj != null)
            {
                p.writeByte(petObj.getUniqueId());
                p.writeInt(petObj.getItemId()); // petid
                p.writeString(petObj.Name);
                p.writeByte(petObj.Level); // pet level
                p.writeShort(petObj.Tameness); // pet tameness
                p.writeByte(petObj.Fullness); // pet fullness
                p.writeShort(0);
                p.writeInt(inv != null ? inv.getItemId() : 0);
            }
        }
        p.writeByte(0); //end of pets


        Item? mount;     //mounts can potentially crash the client if the player's level is not properly checked
        if (chr.MountModel != null 
            && (mount = chr.getInventory(InventoryType.EQUIPPED).getItem(-18)) != null 
            && ItemInformationProvider.getInstance().getEquipLevelReq(mount.getItemId()) <= chr.getLevel())
        {
            p.writeByte(chr.MountModel.getId()); //mount
            p.writeInt(chr.MountModel.getLevel()); //level
            p.writeInt(chr.MountModel.getExp()); //exp
            p.writeInt(chr.MountModel.getTiredness()); //tiredness
        }
        else
        {
            p.writeByte(0);
        }
        p.writeByte(chr.getCashShop().getWishList().Count);
        foreach (int sn in chr.getCashShop().getWishList())
        {
            p.writeInt(sn);
        }

        MonsterBook book = chr.getMonsterBook();
        p.writeInt(book.getBookLevel());
        p.writeInt(book.getNormalCard());
        p.writeInt(book.getSpecialCard());
        p.writeInt(book.getTotalCards());
        p.writeInt(chr.getMonsterBookCover() > 0 ? ItemInformationProvider.getInstance().getCardMobId(chr.getMonsterBookCover()) : 0);
        var medal = chr.getInventory(InventoryType.EQUIPPED).getItem(-49);
        if (medal != null)
        {
            p.writeInt(medal.getItemId());
        }
        else
        {
            p.writeInt(0);
        }
        List<short> medalQuests = new();
        List<QuestStatus> completed = chr.getCompletedQuests();
        foreach (QuestStatus qs in completed)
        {
            if (qs.getQuest().getId() >= 29000)
            { // && q.getQuest().getId() <= 29923
                medalQuests.Add(qs.getQuest().getId());
            }
        }

        medalQuests.Sort();
        p.writeShort(medalQuests.Count);
        foreach (short s in medalQuests)
        {
            p.writeShort(s);
        }
        return p;
    }

    /**
     * It is important that statups is in the correct order (see declaration
     * order in BuffStat) since this method doesn't do automagical
     * reordering.
     *
     * @param buffid
     * @param bufflength
     * @param statups
     * @return
     */
    //1F 00 00 00 00 00 03 00 00 40 00 00 00 E0 00 00 00 00 00 00 00 00 E0 01 8E AA 4F 00 00 C2 EB 0B E0 01 8E AA 4F 00 00 C2 EB 0B 0C 00 8E AA 4F 00 00 C2 EB 0B 44 02 8E AA 4F 00 00 C2 EB 0B 44 02 8E AA 4F 00 00 C2 EB 0B 00 00 E0 7A 1D 00 8E AA 4F 00 00 00 00 00 00 00 00 03
    public static Packet giveBuff(int buffid, int bufflength, params BuffStatValue[] statups)
    {
        OutPacket p = OutPacket.create(SendOpcode.GIVE_BUFF);
        bool special = false;
        writeLongMask(p, statups);
        foreach (var statup in statups)
        {
            if (statup.BuffState.Equals(BuffStat.MONSTER_RIDING) || statup.BuffState.Equals(BuffStat.HOMING_BEACON))
            {
                special = true;
            }
            p.writeShort(statup.Value);
            p.writeInt(buffid);
            p.writeInt(bufflength);
        }
        p.writeInt(0);
        p.writeByte(0);
        p.writeInt(statups[0].Value); //Homing beacon ...

        if (special)
        {
            p.skip(3);
        }
        return p;
    }

    /**
     * @param cid
     * @param statups
     * @param mount
     * @return
     */
    public static Packet showMonsterRiding(int cid, IMount mount)
    { //Gtfo with this, this is just giveForeignBuff
        OutPacket p = OutPacket.create(SendOpcode.GIVE_FOREIGN_BUFF);
        p.writeInt(cid);
        p.writeLong(BuffStat.MONSTER_RIDING.getValue());
        p.writeLong(0);
        p.writeShort(0);
        p.writeInt(mount.getItemId());
        p.writeInt(mount.getSkillId());
        p.writeInt(0); //Server Tick value.
        p.writeShort(0);
        p.writeByte(0); //Times you have been buffed
        return p;
    }
    /*        p.writeInt(cid);
         writeLongMask(mplew, statups);
         foreach(Pair<BuffStat, int> statup in statups) {
         if (morph) {
         p.writeInt(statup.getRight());
         } else {
         p.writeShort(statup.getRight());
         }
         }
         p.writeShort(0);
         p.writeByte(0);*/

    /**
     * @param c
     * @param quest
     * @return
     */
    public static Packet forfeitQuest(short quest)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        p.writeByte(1);
        p.writeShort(quest);
        p.writeByte(0);
        return p;
    }

    /**
     * @param c
     * @param quest
     * @return
     */
    public static Packet completeQuest(short quest, long time)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        p.writeByte(1);
        p.writeShort(quest);
        p.writeByte(2);
        p.writeLong(getTime(time));
        return p;
    }

    /**
     * @param c
     * @param quest
     * @param npc
     * @param progress
     * @return
     */

    public static Packet updateQuestInfo(short quest, int npc)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_QUEST_INFO);
        p.writeByte(8); //0x0A in v95
        p.writeShort(quest);
        p.writeInt(npc);
        p.writeInt(0);
        return p;
    }

    public static Packet addQuestTimeLimit(short quest, int time)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_QUEST_INFO);
        p.writeByte(6);
        p.writeShort(1);//Size but meh, when will there be 2 at the same time? And it won't even replace the old one :)
        p.writeShort(quest);
        p.writeInt(time);
        return p;
    }

    public static Packet removeQuestTimeLimit(short quest)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_QUEST_INFO);
        p.writeByte(7);
        p.writeShort(1);//Position
        p.writeShort(quest);
        return p;
    }

    public static Packet updateQuest(IPlayer chr, QuestStatus qs, bool infoUpdate)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        p.writeByte(1);
        if (infoUpdate)
        {
            QuestStatus iqs = chr.getQuest(qs.getInfoNumber());
            p.writeShort(iqs.getQuestID());
            p.writeByte(1);
            p.writeString(iqs.getProgressData());
        }
        else
        {
            p.writeShort(qs.getQuest().getId());
            p.writeByte((int)qs.getStatus());
            p.writeString(qs.getProgressData());
        }
        p.skip(5);
        return p;
    }

    private static void writeLongMaskD(OutPacket p, List<KeyValuePair<Disease, int>> statups)
    {
        long firstmask = 0;
        long secondmask = 0;
        foreach (var statup in statups)
        {
            if (statup.Key.isFirst())
            {
                firstmask |= (long)statup.Key.getValue();
            }
            else
            {
                secondmask |= (long)statup.Key.getValue();
            }
        }
        p.writeLong(firstmask);
        p.writeLong(secondmask);
    }

    public static Packet giveDebuff(List<KeyValuePair<Disease, int>> statups, MobSkill skill)
    {
        OutPacket p = OutPacket.create(SendOpcode.GIVE_BUFF);
        writeLongMaskD(p, statups);
        foreach (var statup in statups)
        {
            p.writeShort(statup.Value);
            writeMobSkillId(p, skill.getId());
            p.writeInt((int)skill.getDuration());
        }
        p.writeShort(0); // ??? wk charges have 600 here o.o
        p.writeShort(900);//Delay
        p.writeByte(1);
        return p;
    }

    public static Packet giveForeignDebuff(int chrId, List<KeyValuePair<Disease, int>> statups, MobSkill skill)
    {
        // Poison damage visibility and missing diseases status visibility, extended through map transitions thanks to Ronan
        OutPacket p = OutPacket.create(SendOpcode.GIVE_FOREIGN_BUFF);
        p.writeInt(chrId);
        writeLongMaskD(p, statups);
        foreach (var statup in statups)
        {
            if (statup.Key == Disease.POISON)
            {
                p.writeShort(statup.Value);
            }
            writeMobSkillId(p, skill.getId());
        }
        p.writeShort(0); // same as give_buff
        p.writeShort(900);//Delay
        return p;
    }

    public static Packet cancelForeignFirstDebuff(int cid, long mask)
    {
        OutPacket p = OutPacket.create(SendOpcode.CANCEL_FOREIGN_BUFF);
        p.writeInt(cid);
        p.writeLong(mask);
        p.writeLong(0);
        return p;
    }

    public static Packet cancelForeignDebuff(int cid, long mask)
    {
        OutPacket p = OutPacket.create(SendOpcode.CANCEL_FOREIGN_BUFF);
        p.writeInt(cid);
        p.writeLong(0);
        p.writeLong(mask);
        return p;
    }

    public static Packet giveForeignBuff(int chrId, params BuffStatValue[] statups)
    {
        OutPacket p = OutPacket.create(SendOpcode.GIVE_FOREIGN_BUFF);
        p.writeInt(chrId);
        writeLongMask(p, statups);
        foreach (var statup in statups)
        {
            p.writeShort(statup.Value);
        }
        p.writeInt(0);
        p.writeShort(0);
        return p;
    }

    public static Packet cancelForeignBuff(int chrId, List<BuffStat> statups)
    {
        OutPacket p = OutPacket.create(SendOpcode.CANCEL_FOREIGN_BUFF);
        p.writeInt(chrId);
        writeLongMaskFromList(p, statups);
        return p;
    }

    public static Packet cancelBuff(List<BuffStat> statups)
    {
        OutPacket p = OutPacket.create(SendOpcode.CANCEL_BUFF);
        writeLongMaskFromList(p, statups);
        p.writeByte(1);//?
        return p;
    }

    private static void writeLongMask(OutPacket p, params BuffStatValue[] statups)
    {
        long firstmask = 0;
        long secondmask = 0;
        foreach (var statup in statups)
        {
            if (statup.BuffState.IsFirst)
            {
                firstmask |= statup.BuffState.getValue();
            }
            else
            {
                secondmask |= statup.BuffState.getValue();
            }
        }
        p.writeLong(firstmask);
        p.writeLong(secondmask);
    }

    private static void writeLongMaskFromList(OutPacket p, List<BuffStat> statups)
    {
        long firstmask = 0;
        long secondmask = 0;
        foreach (BuffStat statup in statups)
        {
            if (statup.IsFirst)
            {
                firstmask |= statup.getValue();
            }
            else
            {
                secondmask |= statup.getValue();
            }
        }
        p.writeLong(firstmask);
        p.writeLong(secondmask);
    }

    private static void writeLongEncodeTemporaryMask(OutPacket p, ICollection<MonsterStatus> stati)
    {
        int[] masks = new int[4];

        foreach (MonsterStatus statup in stati)
        {
            int pos = statup.isFirst() ? 0 : 2;
            for (int i = 0; i < 2; i++)
            {
                masks[pos + i] |= statup.getValue() >> 32 * i;
            }
        }

        foreach (int mask in masks)
        {
            p.writeInt(mask);
        }
    }

    public static Packet cancelDebuff(long mask)
    {
        OutPacket p = OutPacket.create(SendOpcode.CANCEL_BUFF);
        p.writeLong(0);
        p.writeLong(mask);
        p.writeByte(0);
        return p;
    }

    private static void writeLongMaskSlowD(OutPacket p)
    {
        p.writeInt(0);
        p.writeInt(2048);
        p.writeLong(0);
    }

    public static Packet giveForeignSlowDebuff(int chrId, List<KeyValuePair<Disease, int>> statups, MobSkill skill)
    {
        OutPacket p = OutPacket.create(SendOpcode.GIVE_FOREIGN_BUFF);
        p.writeInt(chrId);
        writeLongMaskSlowD(p);
        foreach (var statup in statups)
        {
            if (statup.Key == Disease.POISON)
            {
                p.writeShort(statup.Value);
            }
            writeMobSkillId(p, skill.getId());
        }
        p.writeShort(0); // same as give_buff
        p.writeShort(900);//Delay
        return p;
    }

    public static Packet cancelForeignSlowDebuff(int chrId)
    {
        OutPacket p = OutPacket.create(SendOpcode.CANCEL_FOREIGN_BUFF);
        p.writeInt(chrId);
        writeLongMaskSlowD(p);
        return p;
    }

    private static void writeLongMaskChair(OutPacket p)
    {
        p.writeInt(0);
        p.writeInt(262144);
        p.writeLong(0);
    }

    public static Packet giveForeignChairSkillEffect(int cid)
    {
        OutPacket p = OutPacket.create(SendOpcode.GIVE_FOREIGN_BUFF);
        p.writeInt(cid);
        writeLongMaskChair(p);

        p.writeShort(0);
        p.writeShort(0);
        p.writeShort(100);
        p.writeShort(1);

        p.writeShort(0);
        p.writeShort(900);

        p.skip(7);

        return p;
    }

    // packet found thanks to Ronan
    public static Packet giveForeignWKChargeEffect(int cid, int buffid, params BuffStatValue[] statups)
    {
        OutPacket p = OutPacket.create(SendOpcode.GIVE_FOREIGN_BUFF);
        p.writeInt(cid);
        writeLongMask(p, statups);
        p.writeInt(buffid);
        p.writeShort(600);
        p.writeShort(1000);//Delay
        p.writeByte(1);
        return p;
    }

    public static Packet cancelForeignChairSkillEffect(int chrId)
    {
        OutPacket p = OutPacket.create(SendOpcode.CANCEL_FOREIGN_BUFF);
        p.writeInt(chrId);
        writeLongMaskChair(p);
        return p;
    }

    public static Packet getPlayerShopChat(IPlayer chr, string chat, bool owner)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.CHAT.getCode());
        p.writeByte(PlayerInteractionHandler.Action.CHAT_THING.getCode());
        p.writeBool(!owner);
        p.writeString(chr.getName() + " : " + chat);
        return p;
    }

    public static Packet getPlayerShopNewVisitor(IPlayer chr, int slot)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.VISIT.getCode());
        p.writeByte(slot);
        addCharLook(p, chr, false);
        p.writeString(chr.getName());
        return p;
    }

    public static Packet getPlayerShopRemoveVisitor(int slot)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.EXIT.getCode());
        if (slot != 0)
        {
            p.writeShort(slot);
        }
        return p;
    }

    public static Packet getTradePartnerAdd(IPlayer chr)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.VISIT.getCode());
        p.writeByte(1);
        addCharLook(p, chr, false);
        p.writeString(chr.getName());
        return p;
    }

    public static Packet tradeInvite(IPlayer chr)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.INVITE.getCode());
        p.writeByte(3);
        p.writeString(chr.getName());
        p.writeBytes(new byte[] { 0xB7, 0x50, 0, 0 });
        return p;
    }

    public static Packet getTradeMesoSet(byte number, int meso)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.SET_MESO.getCode());
        p.writeByte(number);
        p.writeInt(meso);
        return p;
    }

    public static Packet getTradeItemAdd(byte number, Item item)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.SET_ITEMS.getCode());
        p.writeByte(number);
        p.writeByte(item.getPosition());
        addItemInfo(p, item, true);
        return p;
    }

    public static Packet getPlayerShopItemUpdate(PlayerShop shop)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.UPDATE_MERCHANT.getCode());
        p.writeByte(shop.getItems().Count);
        foreach (PlayerShopItem item in shop.getItems())
        {
            p.writeShort(item.getBundles());
            p.writeShort(item.getItem().getQuantity());
            p.writeInt(item.getPrice());
            addItemInfo(p, item.getItem(), true);
        }
        return p;
    }

    public static Packet getPlayerShopOwnerUpdate(PlayerShop.SoldItem item, int position)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.UPDATE_PLAYERSHOP.getCode());
        p.writeByte(position);
        p.writeShort(item.getQuantity());
        p.writeString(item.getBuyer());

        return p;
    }

    /**
     * @param c
     * @param shop
     * @param owner
     * @return
     */
    public static Packet getPlayerShop(PlayerShop shop, bool owner)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.ROOM.getCode());
        p.writeByte(4);
        p.writeByte(4);
        p.writeByte(owner ? 0 : 1);

        if (owner)
        {
            List<PlayerShop.SoldItem> sold = shop.getSold();
            p.writeByte(sold.Count);
            foreach (PlayerShop.SoldItem s in sold)
            {
                p.writeInt(s.getItemId());
                p.writeShort(s.getQuantity());
                p.writeInt(s.getMesos());
                p.writeString(s.getBuyer());
            }
        }
        else
        {
            p.writeByte(0);
        }

        addCharLook(p, shop.getOwner(), false);
        p.writeString(shop.getOwner().getName());

        var visitors = shop.getVisitors();
        for (int i = 0; i < 3; i++)
        {
            if (visitors[i] != null)
            {
                p.writeByte(i + 1);
                addCharLook(p, visitors[i]!, false);
                p.writeString(visitors[i]!.getName());
            }
        }

        p.writeByte(0xFF);
        p.writeString(shop.getDescription());
        List<PlayerShopItem> items = shop.getItems();
        p.writeByte(0x10);  //TODO SLOTS, which is 16 for most stores...slotMax
        p.writeByte(items.Count);
        foreach (PlayerShopItem item in items)
        {
            p.writeShort(item.getBundles());
            p.writeShort(item.getItem().getQuantity());
            p.writeInt(item.getPrice());
            addItemInfo(p, item.getItem(), true);
        }
        return p;
    }

    public static Packet getTradeStart(IClient c, Trade trade, byte number)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.ROOM.getCode());
        p.writeByte(3);
        p.writeByte(2);
        p.writeByte(number);
        if (number == 1)
        {
            p.writeByte(0);
            addCharLook(p, trade.getPartner().getChr(), false);
            p.writeString(trade.getPartner().getChr().getName());
        }
        p.writeByte(number);
        addCharLook(p, c.OnlinedCharacter, false);
        p.writeString(c.OnlinedCharacter.getName());
        p.writeByte(0xFF);
        return p;
    }

    public static Packet getTradeConfirmation()
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.CONFIRM.getCode());
        return p;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="number"></param>
    /// <param name="operation">
    /// <para>2: Trade cancelled, by the other character</para>
    /// <para>7: Trade successful</para>
    /// <para>8: Trade unsuccessful</para>
    /// <para>9: Cannot carry more one-of-a-kind items</para>
    /// <para>12: Cannot trade on different maps</para>
    /// <para>13: Cannot trade, game files damaged</para>
    /// </param>
    /// <returns></returns>
    public static Packet getTradeResult(byte number, byte operation)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.EXIT.getCode());
        p.writeByte(number);
        p.writeByte(operation);
        return p;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="npc">Npcid</param>
    /// <param name="msgType"></param>
    /// <param name="talk"></param>
    /// <param name="endBytes"></param>
    /// <param name="speaker">
    /// <para>0: Npc talking (left)</para>
    /// <para>1: Npc talking (right)</para>
    /// <para>2: Player talking (left)</para>
    /// <para>3: Player talking (left)£¿£¿</para>
    /// </param>
    /// <returns></returns>
    public static Packet getNPCTalk(int npc, byte msgType, string talk, string endBytes, byte speaker)
    {
        OutPacket p = OutPacket.create(SendOpcode.NPC_TALK);
        p.writeByte(4); // ?
        p.writeInt(npc);
        p.writeByte(msgType);
        p.writeByte(speaker);
        p.writeString(talk);
        p.writeBytes(HexTool.toBytes(endBytes));
        return p;
    }

    public static Packet getDimensionalMirror(string talk)
    {
        OutPacket p = OutPacket.create(SendOpcode.NPC_TALK);
        p.writeByte(4); // ?
        p.writeInt(NpcId.DIMENSIONAL_MIRROR);
        p.writeByte(0x0E);
        p.writeByte(0);
        p.writeInt(0);
        p.writeString(talk);
        return p;
    }

    public static Packet getNPCTalkStyle(int npc, string talk, int[] styles)
    {
        OutPacket p = OutPacket.create(SendOpcode.NPC_TALK);
        p.writeByte(4); // ?
        p.writeInt(npc);
        p.writeByte(7);
        p.writeByte(0); //speaker
        p.writeString(talk);
        p.writeByte(styles.Length);
        foreach (int style in styles)
        {
            p.writeInt(style);
        }
        return p;
    }

    public static Packet getNPCTalkNum(int npc, string talk, int def, int min, int max)
    {
        OutPacket p = OutPacket.create(SendOpcode.NPC_TALK);
        p.writeByte(4); // ?
        p.writeInt(npc);
        p.writeByte(3);
        p.writeByte(0); //speaker
        p.writeString(talk);
        p.writeInt(def);
        p.writeInt(min);
        p.writeInt(max);
        p.writeInt(0);
        return p;
    }

    public static Packet getNPCTalkText(int npc, string talk, string def)
    {
        OutPacket p = OutPacket.create(SendOpcode.NPC_TALK);
        p.writeByte(4); // Doesn't matter
        p.writeInt(npc);
        p.writeByte(2);
        p.writeByte(0); //speaker
        p.writeString(talk);
        p.writeString(def);//:D
        p.writeInt(0);
        return p;
    }

    // NPC Quiz packets thanks to Eric
    public static Packet OnAskQuiz(int nSpeakerTypeID, int nSpeakerTemplateID, int nResCode, string sTitle, string sProblemText, string sHintText, int nMinInput, int nMaxInput, int tRemainInitialQuiz)
    {
        OutPacket p = OutPacket.create(SendOpcode.NPC_TALK);
        p.writeByte(nSpeakerTypeID);
        p.writeInt(nSpeakerTemplateID);
        p.writeByte(0x6);
        p.writeByte(0);
        p.writeByte(nResCode);
        if (nResCode == 0x0)
        {//fail has no bytes <3
            p.writeString(sTitle);
            p.writeString(sProblemText);
            p.writeString(sHintText);
            p.writeShort(nMinInput);
            p.writeShort(nMaxInput);
            p.writeInt(tRemainInitialQuiz);
        }
        return p;
    }

    public static Packet OnAskSpeedQuiz(int nSpeakerTypeID, int nSpeakerTemplateID, int nResCode, int nType, int dwAnswer, int nCorrect, int nRemain, int tRemainInitialQuiz)
    {
        OutPacket p = OutPacket.create(SendOpcode.NPC_TALK);
        p.writeByte(nSpeakerTypeID);
        p.writeInt(nSpeakerTemplateID);
        p.writeByte(0x7);
        p.writeByte(0);
        p.writeByte(nResCode);
        if (nResCode == 0x0)
        {//fail has no bytes <3
            p.writeInt(nType);
            p.writeInt(dwAnswer);
            p.writeInt(nCorrect);
            p.writeInt(nRemain);
            p.writeInt(tRemainInitialQuiz);
        }
        return p;
    }

    public static Packet showBuffEffect(int chrId, int skillId, int effectId)
    {
        return showBuffEffect(chrId, skillId, effectId, 3);
    }

    public static Packet showBuffEffect(int chrId, int skillId, int effectId, byte direction)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_FOREIGN_EFFECT);
        p.writeInt(chrId);
        p.writeByte(effectId); //buff level
        p.writeInt(skillId);
        p.writeByte(direction);
        p.writeByte(1);
        p.writeLong(0);
        return p;
    }

    public static Packet showBuffEffect(int chrId, int skillId, int skillLv, int effectId, byte direction)
    {   // updated packet structure found thanks to Rien dev team
        OutPacket p = OutPacket.create(SendOpcode.SHOW_FOREIGN_EFFECT);
        p.writeInt(chrId);
        p.writeByte(effectId);
        p.writeInt(skillId);
        p.writeByte(0);
        p.writeByte(skillLv);
        p.writeByte(direction);
        return p;
    }

    public static Packet showOwnBuffEffect(int skillId, int effectId)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_ITEM_GAIN_INCHAT);
        p.writeByte(effectId);
        p.writeInt(skillId);
        p.writeByte(0xA9);
        p.writeByte(1);
        return p;
    }

    public static Packet showOwnBerserk(int skilllevel, bool Berserk)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_ITEM_GAIN_INCHAT);
        p.writeByte(1);
        p.writeInt(1320006);
        p.writeByte(0xA9);
        p.writeByte(skilllevel);
        p.writeByte(Berserk ? 1 : 0);
        return p;
    }

    public static Packet showBerserk(int chrId, int skillLv, bool berserk)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_FOREIGN_EFFECT);
        p.writeInt(chrId);
        p.writeByte(1);
        p.writeInt(1320006);
        p.writeByte(0xA9);
        p.writeByte(skillLv);
        p.writeBool(berserk);
        return p;
    }

    public static Packet updateSkill(int skillId, int level, int masterlevel, long expiration)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_SKILLS);
        p.writeByte(1);
        p.writeShort(1);
        p.writeInt(skillId);
        p.writeInt(level);
        p.writeInt(masterlevel);
        addExpirationTime(p, expiration);
        p.writeByte(4);
        return p;
    }

    public static Packet getShowQuestCompletion(int id)
    {
        OutPacket p = OutPacket.create(SendOpcode.QUEST_CLEAR);
        p.writeShort(id);
        return p;
    }

    public static Packet getKeymap(Dictionary<int, KeyBinding> keybindings)
    {
        OutPacket p = OutPacket.create(SendOpcode.KEYMAP);
        p.writeByte(0);
        for (int x = 0; x < 90; x++)
        {
            KeyBinding? binding = keybindings.GetValueOrDefault(x);
            if (binding != null)
            {
                p.writeByte(binding.getType());
                p.writeInt(binding.getAction());
            }
            else
            {
                p.writeByte(0);
                p.writeInt(0);
            }
        }
        return p;
    }

    public static Packet QuickslotMappedInit(QuickslotBinding pQuickslot)
    {
        OutPacket p = OutPacket.create(SendOpcode.QUICKSLOT_INIT);
        pQuickslot.encode(p);
        return p;
    }

    public static Packet getInventoryFull()
    {
        return modifyInventory(true, []);
    }

    public static Packet getShowInventoryFull()
    {
        return getShowInventoryStatus(0xff);
    }

    public static Packet showItemUnavailable()
    {
        return getShowInventoryStatus(0xfe);
    }

    public static Packet getShowInventoryStatus(int mode)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        p.writeByte(0);
        p.writeByte(mode);
        p.writeInt(0);
        p.writeInt(0);
        return p;
    }

    public static Packet getStorage(int npcId, byte slots, ICollection<Item> items, int meso)
    {
        OutPacket p = OutPacket.create(SendOpcode.STORAGE);
        p.writeByte(0x16);
        p.writeInt(npcId);
        p.writeByte(slots);
        p.writeShort(0x7E);
        p.writeShort(0);
        p.writeInt(0);
        p.writeInt(meso);
        p.writeShort(0);
        p.writeByte((byte)items.Count);
        foreach (Item item in items)
        {
            addItemInfo(p, item, true);
        }
        p.writeShort(0);
        p.writeByte(0);
        return p;
    }

    /*
     * 0x0A = Inv full
     * 0x0B = You do not have enough mesos
     * 0x0C = One-Of-A-Kind error
     */
    public static Packet getStorageError(byte i)
    {
        OutPacket p = OutPacket.create(SendOpcode.STORAGE);
        p.writeByte(i);
        return p;
    }

    public static Packet mesoStorage(byte slots, int meso)
    {
        OutPacket p = OutPacket.create(SendOpcode.STORAGE);
        p.writeByte(0x13);
        p.writeByte(slots);
        p.writeShort(2);
        p.writeShort(0);
        p.writeInt(0);
        p.writeInt(meso);
        return p;
    }

    public static Packet storeStorage(byte slots, InventoryType type, ICollection<Item> items)
    {
        OutPacket p = OutPacket.create(SendOpcode.STORAGE);
        p.writeByte(0xD);
        p.writeByte(slots);
        p.writeShort(type.getBitfieldEncoding());
        p.writeShort(0);
        p.writeInt(0);
        p.writeByte(items.Count());
        foreach (Item item in items)
        {
            addItemInfo(p, item, true);
        }
        return p;
    }

    public static Packet takeOutStorage(byte slots, InventoryType type, ICollection<Item> items)
    {
        OutPacket p = OutPacket.create(SendOpcode.STORAGE);
        p.writeByte(0x9);
        p.writeByte(slots);
        p.writeShort(type.getBitfieldEncoding());
        p.writeShort(0);
        p.writeInt(0);
        p.writeByte(items.Count());
        foreach (Item item in items)
        {
            addItemInfo(p, item, true);
        }
        return p;
    }

    public static Packet arrangeStorage(byte slots, ICollection<Item> items)
    {
        OutPacket p = OutPacket.create(SendOpcode.STORAGE);
        p.writeByte(0xF);
        p.writeByte(slots);
        p.writeByte(124);
        p.skip(10);
        p.writeByte(items.Count());
        foreach (Item item in items)
        {
            addItemInfo(p, item, true);
        }
        p.writeByte(0);
        return p;
    }

    /**
     * @param oid
     * @param remhppercentage
     * @return
     */
    public static Packet showMonsterHP(int oid, int remhppercentage)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_MONSTER_HP);
        p.writeInt(oid);
        p.writeByte(remhppercentage);
        return p;
    }

    public static Packet showBossHP(int oid, int currHP, int maxHP, byte tagColor, byte tagBgColor)
    {
        OutPacket p = OutPacket.create(SendOpcode.FIELD_EFFECT);
        p.writeByte(5);
        p.writeInt(oid);
        p.writeInt(currHP);
        p.writeInt(maxHP);
        p.writeByte(tagColor);
        p.writeByte(tagBgColor);
        return p;
    }

    private static HpMpPair normalizedCustomMaxHP(long currHP, long maxHP)
    {
        int sendHP, sendMaxHP;

        if (maxHP <= int.MaxValue)
        {
            sendHP = (int)currHP;
            sendMaxHP = (int)maxHP;
        }
        else
        {
            float f = ((float)currHP) / maxHP;

            sendHP = (int)(int.MaxValue * f);
            sendMaxHP = int.MaxValue;
        }

        return new(sendHP, sendMaxHP);
    }

    public static Packet customShowBossHP(byte call, int oid, long currHP, long maxHP, byte tagColor, byte tagBgColor)
    {
        var customHP = normalizedCustomMaxHP(currHP, maxHP);

        OutPacket p = OutPacket.create(SendOpcode.FIELD_EFFECT);
        p.writeByte(call);
        p.writeInt(oid);
        p.writeInt(customHP.Hp);
        p.writeInt(customHP.Mp);
        p.writeByte(tagColor);
        p.writeByte(tagBgColor);
        return p;
    }

    public static Packet giveFameResponse(int mode, string charname, int newfame)
    {
        OutPacket p = OutPacket.create(SendOpcode.FAME_RESPONSE);
        p.writeByte(0);
        p.writeString(charname);
        p.writeByte(mode);
        p.writeShort(newfame);
        p.writeShort(0);
        return p;
    }

    /**
     * status can be: <br> 0: ok, use giveFameResponse<br> 1: the username is
     * incorrectly entered<br> 2: users under level 15 are unable to toggle with
     * fame.<br> 3: can't raise or drop fame anymore today.<br> 4: can't raise
     * or drop fame for this character for this month anymore.<br> 5: received
     * fame, use receiveFame()<br> 6: level of fame neither has been raised nor
     * dropped due to an unexpected error
     *
     * @param status
     * @return
     */
    public static Packet giveFameErrorResponse(int status)
    {
        OutPacket p = OutPacket.create(SendOpcode.FAME_RESPONSE);
        p.writeByte(status);
        return p;
    }

    public static Packet receiveFame(int mode, string charnameFrom)
    {
        OutPacket p = OutPacket.create(SendOpcode.FAME_RESPONSE);
        p.writeByte(5);
        p.writeString(charnameFrom);
        p.writeByte(mode);
        return p;
    }

    public static Packet partyCreated(ITeam party, int partycharid)
    {
        OutPacket p = OutPacket.create(SendOpcode.PARTY_OPERATION);
        p.writeByte(8);
        p.writeInt(party.getId());

        Dictionary<int, Door> partyDoors = party.getDoors();
        if (partyDoors.Count > 0)
        {
            var door = partyDoors.GetValueOrDefault(partycharid);

            if (door != null)
            {
                DoorObject mdo = door.getAreaDoor();
                p.writeInt(mdo.getTo().getId());
                p.writeInt(mdo.getFrom().getId());
                p.writeInt(mdo.getPosition().X);
                p.writeInt(mdo.getPosition().Y);
            }
            else
            {
                p.writeInt(MapId.NONE);
                p.writeInt(MapId.NONE);
                p.writeInt(0);
                p.writeInt(0);
            }
        }
        else
        {
            p.writeInt(MapId.NONE);
            p.writeInt(MapId.NONE);
            p.writeInt(0);
            p.writeInt(0);
        }
        return p;
    }

    public static Packet partyInvite(IPlayer from)
    {
        OutPacket p = OutPacket.create(SendOpcode.PARTY_OPERATION);
        p.writeByte(4);
        p.writeInt(from.getParty()!.getId());
        p.writeString(from.getName());
        p.writeByte(0);
        return p;
    }

    public static Packet partySearchInvite(IPlayer from)
    {
        OutPacket p = OutPacket.create(SendOpcode.PARTY_OPERATION);
        p.writeByte(4);
        p.writeInt(from.getParty()!.getId());
        p.writeString("PS: " + from.getName());
        p.writeByte(0);
        return p;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message">
    /// <para>10: A beginner can't create a party.</para>
    /// <para>1/5/6/11/14/19: Your request for a party didn't work due to an unexpected error.</para>
    /// <para>12: Quit as leader of the party.</para>
    /// <para>13: You have yet to join a party.</para>
    /// <para>16: Already have joined a party.</para>
    /// <para>17: The party you're trying to join is already in full capacity.</para>
    /// <para>19: Unable to find the requested character in this channel.</para>
    /// <para>25: Cannot kick another user in this map.</para>
    /// <para>28/29: Leadership can only be given to a party member in the vicinity.</para>
    /// <para>30: Change leadership only on same channel.</para>
    /// </param>
    /// <returns></returns>
    public static Packet partyStatusMessage(int message)
    {
        OutPacket p = OutPacket.create(SendOpcode.PARTY_OPERATION);
        p.writeByte(message);
        return p;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="message">
    /// <para>21: Player is blocking any party invitations</para>
    /// <para>22: Player is taking care of another invitation</para>
    /// <para>23: Player have denied request to the party.</para>
    /// </param>
    /// <param name="charname"></param>
    /// <returns></returns>
    public static Packet partyStatusMessage(int message, string charname)
    {
        OutPacket p = OutPacket.create(SendOpcode.PARTY_OPERATION);
        p.writeByte(message);
        p.writeString(charname);
        return p;
    }

    private static void addPartyStatus(int forchannel, ITeam party, OutPacket p, bool leaving)
    {
        var partymembers = party.getMembers();
        while (partymembers.Count < 6)
        {
            partymembers.Add(new Player());
        }
        foreach (var partychar in partymembers)
        {
            p.writeInt(partychar.getId());
        }
        foreach (var partychar in partymembers)
        {
            p.writeFixedString(partychar.getName());
        }
        foreach (var partychar in partymembers)
        {
            p.writeInt(partychar.JobId);
        }
        foreach (var partychar in partymembers)
        {
            p.writeInt(partychar.getLevel());
        }
        foreach (var partychar in partymembers)
        {
            if (partychar.IsOnlined)
            {
                p.writeInt(partychar.Channel!.Value - 1);
            }
            else
            {
                p.writeInt(-2);
            }
        }
        p.writeInt(party.getLeader().getId());
        foreach (var partychar in partymembers)
        {
            if (partychar.Channel == forchannel)
            {
                p.writeInt(partychar.getMapId());
            }
            else
            {
                p.writeInt(0);
            }
        }

        Dictionary<int, Door> partyDoors = party.getDoors();
        foreach (var partychar in partymembers)
        {
            if (partychar.Channel == forchannel && !leaving)
            {
                if (partyDoors.Count > 0)
                {
                    var door = partyDoors.GetValueOrDefault(partychar.getId());
                    if (door != null)
                    {
                        DoorObject mdo = door.getTownDoor();
                        p.writeInt(mdo.getTown().getId());
                        p.writeInt(mdo.getArea().getId());
                        p.writeInt(mdo.getPosition().X);
                        p.writeInt(mdo.getPosition().Y);
                    }
                    else
                    {
                        p.writeInt(MapId.NONE);
                        p.writeInt(MapId.NONE);
                        p.writeInt(0);
                        p.writeInt(0);
                    }
                }
                else
                {
                    p.writeInt(MapId.NONE);
                    p.writeInt(MapId.NONE);
                    p.writeInt(0);
                    p.writeInt(0);
                }
            }
            else
            {
                p.writeInt(MapId.NONE);
                p.writeInt(MapId.NONE);
                p.writeInt(0);
                p.writeInt(0);
            }
        }
    }

    public static Packet updateParty(int forChannel, ITeam party, PartyOperation op, IPlayer target)
    {
        OutPacket p = OutPacket.create(SendOpcode.PARTY_OPERATION);
        switch (op)
        {
            case PartyOperation.DISBAND:
            case PartyOperation.EXPEL:
            case PartyOperation.LEAVE:
                p.writeByte(0x0C);
                p.writeInt(party.getId());
                p.writeInt(target.getId());
                if (op == PartyOperation.DISBAND)
                {
                    p.writeByte(0);
                    p.writeInt(party.getId());
                }
                else
                {
                    p.writeByte(1);
                    if (op == PartyOperation.EXPEL)
                    {
                        p.writeByte(1);
                    }
                    else
                    {
                        p.writeByte(0);
                    }
                    p.writeString(target.getName());
                    addPartyStatus(forChannel, party, p, false);
                }
                break;
            case PartyOperation.JOIN:
                p.writeByte(0xF);
                p.writeInt(party.getId());
                p.writeString(target.getName());
                addPartyStatus(forChannel, party, p, false);
                break;
            case PartyOperation.SILENT_UPDATE:
            case PartyOperation.LOG_ONOFF:
                p.writeByte(0x7);
                p.writeInt(party.getId());
                addPartyStatus(forChannel, party, p, false);
                break;
            case PartyOperation.CHANGE_LEADER:
                p.writeByte(0x1B);
                p.writeInt(target.getId());
                p.writeByte(0);
                break;
        }
        return p;
    }

    public static Packet partyPortal(int townId, int targetId, Point position)
    {
        OutPacket p = OutPacket.create(SendOpcode.PARTY_OPERATION);
        p.writeShort(0x23);
        p.writeInt(townId);
        p.writeInt(targetId);
        p.writePos(position);
        return p;
    }

    public static Packet updatePartyMemberHP(int cid, int curhp, int maxhp)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_PARTYMEMBER_HP);
        p.writeInt(cid);
        p.writeInt(curhp);
        p.writeInt(maxhp);
        return p;
    }

    /**
     * mode: 0 buddychat; 1 partychat; 2 guildchat
     *
     * @param name
     * @param chattext
     * @param mode
     * @return
     */
    public static Packet multiChat(string name, string chattext, int mode)
    {
        OutPacket p = OutPacket.create(SendOpcode.MULTICHAT);
        p.writeByte(mode);
        p.writeString(name);
        p.writeString(chattext);
        return p;
    }

    private static void writeIntMask(OutPacket p, Dictionary<MonsterStatus, int> stats)
    {
        int firstmask = 0;
        int secondmask = 0;
        foreach (MonsterStatus stat in stats.Keys)
        {
            if (stat.isFirst())
            {
                firstmask |= stat.getValue();
            }
            else
            {
                secondmask |= stat.getValue();
            }
        }
        p.writeInt(firstmask);
        p.writeInt(secondmask);
    }

    public static Packet applyMonsterStatus(int oid, MonsterStatusEffect mse, List<int>? reflection)
    {
        Dictionary<MonsterStatus, int> stati = mse.getStati();
        OutPacket p = OutPacket.create(SendOpcode.APPLY_MONSTER_STATUS);
        p.writeInt(oid);
        p.writeLong(0);
        writeIntMask(p, stati);
        foreach (var stat in stati)
        {
            p.writeShort(stat.Value);
            if (mse.isMonsterSkill())
            {
                writeMobSkillId(p, mse.getMobSkill()!.getId());
            }
            else
            {
                p.writeInt(mse.getSkill()!.getId());
            }
            p.writeShort(-1); // might actually be the buffTime but it's not displayed anywhere
        }
        int size = stati.Count; // size
        if (reflection != null)
        {
            foreach (int refObj in reflection)
            {
                p.writeInt(refObj);
            }
            if (reflection.Count > 0)
            {
                size /= 2; // This gives 2 buffs per reflection but it's really one buff
            }
        }
        p.writeByte(size); // size
        p.writeInt(0);
        return p;
    }

    public static Packet cancelMonsterStatus(int oid, Dictionary<MonsterStatus, int> stats)
    {
        OutPacket p = OutPacket.create(SendOpcode.CANCEL_MONSTER_STATUS);
        p.writeInt(oid);
        p.writeLong(0);
        writeIntMask(p, stats);
        p.writeInt(0);
        return p;
    }

    public static Packet getClock(int time)
    { // time in seconds
        OutPacket p = OutPacket.create(SendOpcode.CLOCK);
        p.writeByte(2); // clock type. if you send 3 here you have to send another byte (which does not matter at all) before the timestamp
        p.writeInt(time);
        return p;
    }

    public static Packet getClockTime(int hour, int min, int sec)
    { // Current Time
        OutPacket p = OutPacket.create(SendOpcode.CLOCK);
        p.writeByte(1); //Clock-Type
        p.writeByte(hour);
        p.writeByte(min);
        p.writeByte(sec);
        return p;
    }

    public static Packet removeClock()
    {
        OutPacket p = OutPacket.create(SendOpcode.STOP_CLOCK);
        p.writeByte(0);
        return p;
    }

    public static Packet spawnMobMist(int objId, int ownerMobId, MobSkillId msId, MobMist mist)
    {
        return spawnMist(objId, ownerMobId, msId.type.getId(), msId.level, mist);
    }

    public static Packet spawnMist(int objId, int ownerId, int skill, int level, Mist mist)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_MIST);
        p.writeInt(objId);
        p.writeInt(mist.isMobMist() ? 0 : mist.isPoisonMist() ? 1 : mist.isRecoveryMist() ? 4 : 2); // mob mist = 0, player poison = 1, smokescreen = 2, unknown = 3, recovery = 4
        p.writeInt(ownerId);
        p.writeInt(skill);
        p.writeByte(level);
        p.writeShort(mist.getSkillDelay()); // Skill delay
        p.writeInt(mist.getBox().X);
        p.writeInt(mist.getBox().Y);
        p.writeInt(mist.getBox().X + mist.getBox().Width);
        p.writeInt(mist.getBox().Y + mist.getBox().Height);
        p.writeInt(0);
        return p;
    }

    public static Packet removeMist(int objId)
    {
        OutPacket p = OutPacket.create(SendOpcode.REMOVE_MIST);
        p.writeInt(objId);
        return p;
    }

    public static Packet damageSummon(int cid, int oid, int damage, int monsterIdFrom)
    {
        OutPacket p = OutPacket.create(SendOpcode.DAMAGE_SUMMON);
        p.writeInt(cid);
        p.writeInt(oid);
        p.writeByte(12);
        p.writeInt(damage);         // damage display doesn't seem to work...
        p.writeInt(monsterIdFrom);
        p.writeByte(0);
        return p;
    }

    public static Packet damageMonster(int oid, int damage)
    {
        return damageMonster(oid, damage, 0, 0);
    }

    public static Packet healMonster(int oid, int heal, int curhp, int maxhp)
    {
        return damageMonster(oid, -heal, curhp, maxhp);
    }

    private static Packet damageMonster(int oid, int damage, int curhp, int maxhp)
    {
        OutPacket p = OutPacket.create(SendOpcode.DAMAGE_MONSTER);
        p.writeInt(oid);
        p.writeByte(0);
        p.writeInt(damage);
        p.writeInt(curhp);
        p.writeInt(maxhp);
        return p;
    }

    public static Packet updateBuddylist(ICollection<BuddylistEntry> buddylist)
    {
        OutPacket p = OutPacket.create(SendOpcode.BUDDYLIST);
        p.writeByte(7);
        p.writeByte(buddylist.Count());
        foreach (BuddylistEntry buddy in buddylist)
        {
            if (buddy.Visible)
            {
                p.writeInt(buddy.getCharacterId()); // cid
                p.writeFixedString(buddy.getName());
                p.writeByte(0); // opposite status
                p.writeInt(buddy.getChannel() - 1);
                p.writeFixedString(buddy.Group);
                p.writeInt(0);//mapid?
            }
        }
        for (int x = 0; x < buddylist.Count(); x++)
        {
            p.writeInt(0);//mapid?
        }
        return p;
    }

    public static Packet buddylistMessage(byte message)
    {
        OutPacket p = OutPacket.create(SendOpcode.BUDDYLIST);
        p.writeByte(message);
        return p;
    }

    public static Packet requestBuddylistAdd(int chrIdFrom, int chrId, string nameFrom)
    {
        OutPacket p = OutPacket.create(SendOpcode.BUDDYLIST);
        p.writeByte(9);
        p.writeInt(chrIdFrom);
        p.writeString(nameFrom);
        p.writeInt(chrIdFrom);
        p.writeFixedString(nameFrom, 11);
        p.writeByte(0x09);
        p.writeByte(0xf0);
        p.writeByte(0x01);
        p.writeInt(0x0f);
        p.writeFixedString("Default Group");
        p.writeByte(0);
        p.writeInt(chrId);
        return p;
    }

    public static Packet updateBuddyChannel(int characterid, int channel)
    {
        OutPacket p = OutPacket.create(SendOpcode.BUDDYLIST);
        p.writeByte(0x14);
        p.writeInt(characterid);
        p.writeByte(0);
        p.writeInt(channel);
        return p;
    }

    public static Packet itemEffect(int characterid, int itemid)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_ITEM_EFFECT);
        p.writeInt(characterid);
        p.writeInt(itemid);
        return p;
    }

    public static Packet updateBuddyCapacity(int capacity)
    {
        OutPacket p = OutPacket.create(SendOpcode.BUDDYLIST);
        p.writeByte(0x15);
        p.writeByte(capacity);
        return p;
    }

    public static Packet showChair(int characterid, int itemid)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_CHAIR);
        p.writeInt(characterid);
        p.writeInt(itemid);
        return p;
    }

    public static Packet cancelChair(int id)
    {
        OutPacket p = OutPacket.create(SendOpcode.CANCEL_CHAIR);
        if (id < 0)
        {
            p.writeByte(0);
        }
        else
        {
            p.writeByte(1);
            p.writeShort(id);
        }
        return p;
    }

    // is there a way to spawn reactors non-animated?
    public static Packet spawnReactor(Reactor reactor)
    {
        OutPacket p = OutPacket.create(SendOpcode.REACTOR_SPAWN);
        p.writeInt(reactor.getObjectId());
        p.writeInt(reactor.getId());
        p.writeByte(reactor.getState());
        p.writePos(reactor.getPosition());
        p.writeByte(0);
        p.writeShort(0);
        return p;
    }

    // is there a way to trigger reactors without performing the hit animation?
    public static Packet triggerReactor(Reactor reactor, int stance)
    {
        OutPacket p = OutPacket.create(SendOpcode.REACTOR_HIT);
        p.writeInt(reactor.getObjectId());
        p.writeByte(reactor.getState());
        p.writePos(reactor.getPosition());
        p.writeByte(stance);
        p.writeShort(0);
        p.writeByte(5); // frame delay, set to 5 since there doesn't appear to be a fixed formula for it
        return p;
    }

    public static Packet destroyReactor(Reactor reactor)
    {
        OutPacket p = OutPacket.create(SendOpcode.REACTOR_DESTROY);
        p.writeInt(reactor.getObjectId());
        p.writeByte(reactor.getState());
        p.writePos(reactor.getPosition());
        return p;
    }

    public static Packet musicChange(string song)
    {
        return environmentChange(song, 6);
    }

    public static Packet showEffect(string effect)
    {
        return environmentChange(effect, 3);
    }

    public static Packet playSound(string sound)
    {
        return environmentChange(sound, 4);
    }

    public static Packet environmentChange(string env, int mode)
    {
        OutPacket p = OutPacket.create(SendOpcode.FIELD_EFFECT);
        p.writeByte(mode);
        p.writeString(env);
        return p;
    }

    public static Packet environmentMove(string env, int mode)
    {
        OutPacket p = OutPacket.create(SendOpcode.FIELD_OBSTACLE_ONOFF);
        p.writeString(env);
        p.writeInt(mode);   // 0: stop and back to start, 1: move
        return p;
    }

    public static Packet environmentMoveList(IDictionary<string, int> envList)
    {
        OutPacket p = OutPacket.create(SendOpcode.FIELD_OBSTACLE_ONOFF_LIST);
        p.writeInt(envList.Count);

        foreach (var envMove in envList)
        {
            p.writeString(envMove.Key);
            p.writeInt(envMove.Value);
        }

        return p;
    }

    public static Packet environmentMoveReset()
    {
        return OutPacket.create(SendOpcode.FIELD_OBSTACLE_ALL_RESET);
    }

    public static Packet startMapEffect(string msg, int itemId, bool active)
    {
        OutPacket p = OutPacket.create(SendOpcode.BLOW_WEATHER);
        p.writeBool(!active);
        p.writeInt(itemId);
        if (active)
        {
            p.writeString(msg);
        }
        return p;
    }

    public static Packet removeMapEffect()
    {
        OutPacket p = OutPacket.create(SendOpcode.BLOW_WEATHER);
        p.writeByte(0);
        p.writeInt(0);
        return p;
    }

    public static Packet mapEffect(string path)
    {
        OutPacket p = OutPacket.create(SendOpcode.FIELD_EFFECT);
        p.writeByte(3);
        p.writeString(path);
        return p;
    }

    public static Packet mapSound(string path)
    {
        OutPacket p = OutPacket.create(SendOpcode.FIELD_EFFECT);
        p.writeByte(4);
        p.writeString(path);
        return p;
    }

    public static Packet skillEffect(IPlayer from, int skillId, int level, byte flags, int speed, byte direction)
    {
        OutPacket p = OutPacket.create(SendOpcode.SKILL_EFFECT);
        p.writeInt(from.getId());
        p.writeInt(skillId);
        p.writeByte(level);
        p.writeByte(flags);
        p.writeByte(speed);
        p.writeByte(direction); //Mmmk
        return p;
    }

    public static Packet skillCancel(IPlayer from, int skillId)
    {
        OutPacket p = OutPacket.create(SendOpcode.CANCEL_SKILL_EFFECT);
        p.writeInt(from.getId());
        p.writeInt(skillId);
        return p;
    }

    public static Packet catchMonster(int mobOid, byte success)
    {   // updated packet structure found thanks to Rien dev team
        OutPacket p = OutPacket.create(SendOpcode.CATCH_MONSTER);
        p.writeInt(mobOid);
        p.writeByte(success);
        return p;
    }

    public static Packet catchMonster(int mobOid, int itemid, byte success)
    {
        OutPacket p = OutPacket.create(SendOpcode.CATCH_MONSTER_WITH_ITEM);
        p.writeInt(mobOid);
        p.writeInt(itemid);
        p.writeByte(success);
        return p;
    }

    /**
     * Sends a player hint.
     *
     * @param hint   The hint it's going to send.
     * @param width  How tall the box is going to be.
     * @param height How long the box is going to be.
     * @return The player hint packet.
     */
    public static Packet sendHint(string hint, int width, int height)
    {
        if (width < 1)
        {
            width = hint.Length * 10;
            if (width < 40)
            {
                width = 40;
            }
        }
        if (height < 5)
        {
            height = 5;
        }
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_HINT);
        p.writeString(hint);
        p.writeShort(width);
        p.writeShort(height);
        p.writeByte(1);
        return p;
    }

    public static Packet messengerInvite(string from, int messengerid)
    {
        OutPacket p = OutPacket.create(SendOpcode.MESSENGER);
        p.writeByte(0x03);
        p.writeString(from);
        p.writeByte(0);
        p.writeInt(messengerid);
        p.writeByte(0);
        return p;
    }

    /*
    public static Packet sendSpouseChat(IPlayer partner, string msg) {
            OutPacket p = OutPacket.create(SendOpcode);
            SPOUSE_CHAT);
            p.writeString(partner.getName());
            p.writeString(msg);
            return p;
    }
    */

    public static Packet OnCoupleMessage(string fiance, string text, bool spouse)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPOUSE_CHAT);
        p.writeByte(spouse ? 5 : 4); // v2 = CInPacket::Decode1(a1) - 4;
        if (spouse)
        { // if ( v2 ) {
            p.writeString(fiance);
        }
        p.writeByte(spouse ? 5 : 1);
        p.writeString(text);
        return p;
    }

    public static Packet addMessengerPlayer(string from, IPlayer chr, int position, int channel)
    {
        OutPacket p = OutPacket.create(SendOpcode.MESSENGER);
        p.writeByte(0x00);
        p.writeByte(position);
        addCharLook(p, chr, true);
        p.writeString(from);
        p.writeByte(channel);
        p.writeByte(0x00);
        return p;
    }

    public static Packet removeMessengerPlayer(int position)
    {
        OutPacket p = OutPacket.create(SendOpcode.MESSENGER);
        p.writeByte(0x02);
        p.writeByte(position);
        return p;
    }

    public static Packet updateMessengerPlayer(string from, IPlayer chr, int position, int channel)
    {
        OutPacket p = OutPacket.create(SendOpcode.MESSENGER);
        p.writeByte(0x07);
        p.writeByte(position);
        addCharLook(p, chr, true);
        p.writeString(from);
        p.writeByte(channel);
        p.writeByte(0x00);
        return p;
    }

    public static Packet joinMessenger(int position)
    {
        OutPacket p = OutPacket.create(SendOpcode.MESSENGER);
        p.writeByte(0x01);
        p.writeByte(position);
        return p;
    }

    public static Packet messengerChat(string text)
    {
        OutPacket p = OutPacket.create(SendOpcode.MESSENGER);
        p.writeByte(0x06);
        p.writeString(text);
        return p;
    }

    public static Packet messengerNote(string text, int mode, int mode2)
    {
        OutPacket p = OutPacket.create(SendOpcode.MESSENGER);
        p.writeByte(mode);
        p.writeString(text);
        p.writeByte(mode2);
        return p;
    }

    private static void addPetInfo(OutPacket p, Pet pet, bool showpet)
    {
        p.writeByte(1);
        if (showpet)
        {
            p.writeByte(0);
        }

        p.writeInt(pet.getItemId());
        p.writeString(pet.getName());
        p.writeLong(pet.getUniqueId());
        p.writePos(pet.getPos());
        p.writeByte(pet.getStance());
        p.writeInt(pet.getFh());
    }

    public static Packet showPet(IPlayer chr, Pet pet, bool remove, bool hunger)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_PET);
        p.writeInt(chr.getId());
        p.writeByte(chr.getPetIndex(pet));
        if (remove)
        {
            p.writeByte(0);
            p.writeBool(hunger);
        }
        else
        {
            addPetInfo(p, pet, true);
        }
        return p;
    }

    public static Packet movePet(int cid, int pid, sbyte slot, List<LifeMovementFragment> moves)
    {
        OutPacket p = OutPacket.create(SendOpcode.MOVE_PET);
        p.writeInt(cid);
        p.writeSByte(slot);
        p.writeInt(pid);
        serializeMovementList(p, moves);
        return p;
    }

    public static Packet petChat(int cid, sbyte index, int act, string text)
    {
        OutPacket p = OutPacket.create(SendOpcode.PET_CHAT);
        p.writeInt(cid);
        p.writeSByte(index);
        p.writeByte(0);
        p.writeByte(act);
        p.writeString(text);
        p.writeByte(0);
        return p;
    }

    public static Packet petFoodResponse(int cid, sbyte index, bool success, bool balloonType)
    {
        OutPacket p = OutPacket.create(SendOpcode.PET_COMMAND);
        p.writeInt(cid);
        p.writeSByte(index);
        p.writeByte(1);
        p.writeBool(success);
        p.writeBool(balloonType);
        return p;
    }

    public static Packet commandResponse(int cid, sbyte index, bool talk, int animation, bool balloonType)
    {
        OutPacket p = OutPacket.create(SendOpcode.PET_COMMAND);
        p.writeInt(cid);
        p.writeSByte(index);
        p.writeByte(0);
        p.writeByte(animation);
        p.writeBool(!talk);
        p.writeBool(balloonType);
        return p;
    }

    public static Packet showOwnPetLevelUp(sbyte index)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_ITEM_GAIN_INCHAT);
        p.writeByte(4);
        p.writeByte(0);
        p.writeSByte(index); // Pet Index
        return p;
    }

    public static Packet showPetLevelUp(IPlayer chr, sbyte index)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_FOREIGN_EFFECT);
        p.writeInt(chr.getId());
        p.writeByte(4);
        p.writeByte(0);
        p.writeSByte(index);
        return p;
    }

    public static Packet changePetName(IPlayer chr, string newname, int slot)
    {
        OutPacket p = OutPacket.create(SendOpcode.PET_NAMECHANGE);
        p.writeInt(chr.getId());
        p.writeByte(0);
        p.writeString(newname);
        p.writeByte(0);
        return p;
    }

    public static Packet loadExceptionList(int cid, int petId, sbyte petIdx, List<int> data)
    {
        OutPacket p = OutPacket.create(SendOpcode.PET_EXCEPTION_LIST);
        p.writeInt(cid);
        p.writeSByte(petIdx);
        p.writeLong(petId);
        p.writeByte(data.Count);
        foreach (int ids in data)
        {
            p.writeInt(ids);
        }
        return p;
    }

    public static Packet petStatUpdate(IPlayer chr)
    {
        // this actually does nothing... packet structure and stats needs to be uncovered

        OutPacket p = OutPacket.create(SendOpcode.STAT_CHANGED);
        int mask = 0;
        mask |= Stat.PET.getValue();
        p.writeByte(0);
        p.writeInt(mask);
        var pets = chr.getPets();
        for (int i = 0; i < 3; i++)
        {
            if (pets[i] != null)
            {
                p.writeLong(pets[i]!.getUniqueId());
            }
            else
            {
                p.writeLong(0);
            }
        }
        p.writeByte(0);
        return p;
    }

    public static Packet showForcedEquip(int team)
    {
        OutPacket p = OutPacket.create(SendOpcode.FORCED_MAP_EQUIP);
        if (team > -1)
        {
            p.writeByte(team);   // 00 = red, 01 = blue
        }
        return p;
    }

    public static Packet summonSkill(int cid, int summonSkillId, int newStance)
    {
        OutPacket p = OutPacket.create(SendOpcode.SUMMON_SKILL);
        p.writeInt(cid);
        p.writeInt(summonSkillId);
        p.writeByte(newStance);
        return p;
    }

    public static Packet skillCooldown(int sid, int time)
    {
        OutPacket p = OutPacket.create(SendOpcode.COOLDOWN);
        p.writeInt(sid);
        p.writeShort(time);//Int in v97
        return p;
    }

    public static Packet skillBookResult(IPlayer chr, int skillid, int maxlevel, bool canuse, bool success)
    {
        OutPacket p = OutPacket.create(SendOpcode.SKILL_LEARN_ITEM_RESULT);
        p.writeInt(chr.getId());
        p.writeByte(1);
        p.writeInt(skillid);
        p.writeInt(maxlevel);
        p.writeByte(canuse ? 1 : 0);
        p.writeByte(success ? 1 : 0);
        return p;
    }

    public static Packet getMacros(SkillMacro?[] macros)
    {
        OutPacket p = OutPacket.create(SendOpcode.MACRO_SYS_DATA_INIT);
        int count = 0;
        for (int i = 0; i < 5; i++)
        {
            if (macros[i] != null)
            {
                count++;
            }
        }
        p.writeByte(count);
        for (int i = 0; i < 5; i++)
        {
            var macro = macros[i];
            if (macro != null)
            {
                p.writeString(macro.getName());
                p.writeByte(macro.getShout());
                p.writeInt(macro.getSkill1());
                p.writeInt(macro.getSkill2());
                p.writeInt(macro.getSkill3());
            }
        }
        return p;
    }

    public static Packet showAllCharacterInfo(int worldid, List<IPlayer> chars, bool usePic)
    {
        OutPacket p = OutPacket.create(SendOpcode.VIEW_ALL_CHAR);
        p.writeByte(0);
        p.writeByte(worldid);
        p.writeByte(chars.Count);
        foreach (IPlayer chr in chars)
        {
            addCharEntry(p, chr, true);
        }
        p.writeByte(usePic ? 1 : 2);
        return p;
    }

    public static Packet updateMount(int charid, IMount mount, bool levelup)
    {
        OutPacket p = OutPacket.create(SendOpcode.SET_TAMING_MOB_INFO);
        p.writeInt(charid);
        p.writeInt(mount.getLevel());
        p.writeInt(mount.getExp());
        p.writeInt(mount.getTiredness());
        p.writeByte(levelup ? (byte)1 : (byte)0);
        return p;
    }

    public static Packet crogBoatPacket(bool type)
    {
        OutPacket p = OutPacket.create(SendOpcode.CONTI_MOVE);
        p.writeByte(10);
        p.writeByte(type ? 4 : 5);
        return p;
    }

    public static Packet boatPacket(bool type)
    {
        OutPacket p = OutPacket.create(SendOpcode.CONTI_STATE);
        p.writeByte(type ? 1 : 2);
        p.writeByte(0);
        return p;
    }

    public static Packet getMiniGame(IClient c, MiniGame minigame, bool owner, int piece)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.ROOM.getCode());
        p.writeByte(1);
        p.writeByte(0);
        p.writeBool(!owner);
        p.writeByte(0);
        addCharLook(p, minigame.getOwner(), false);
        p.writeString(minigame.getOwner().getName());
        if (minigame.getVisitor() != null)
        {
            IPlayer visitor = minigame.getVisitor();
            p.writeByte(1);
            addCharLook(p, visitor, false);
            p.writeString(visitor.getName());
        }
        p.writeByte(0xFF);
        p.writeByte(0);
        p.writeInt(1);
        p.writeInt(minigame.getOwner().getMiniGamePoints(MiniGameResult.WIN, true));
        p.writeInt(minigame.getOwner().getMiniGamePoints(MiniGameResult.TIE, true));
        p.writeInt(minigame.getOwner().getMiniGamePoints(MiniGameResult.LOSS, true));
        p.writeInt(minigame.getOwnerScore());
        if (minigame.getVisitor() != null)
        {
            IPlayer visitor = minigame.getVisitor();
            p.writeByte(1);
            p.writeInt(1);
            p.writeInt(visitor.getMiniGamePoints(MiniGameResult.WIN, true));
            p.writeInt(visitor.getMiniGamePoints(MiniGameResult.TIE, true));
            p.writeInt(visitor.getMiniGamePoints(MiniGameResult.LOSS, true));
            p.writeInt(minigame.getVisitorScore());
        }
        p.writeByte(0xFF);
        p.writeString(minigame.getDescription());
        p.writeByte(piece);
        p.writeByte(0);
        return p;
    }

    public static Packet getMiniGameReady(MiniGame game)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.READY.getCode());
        return p;
    }

    public static Packet getMiniGameUnReady(MiniGame game)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.UN_READY.getCode());
        return p;
    }

    public static Packet getMiniGameStart(MiniGame game, int loser)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.START.getCode());
        p.writeByte(loser);
        return p;
    }

    public static Packet getMiniGameSkipOwner(MiniGame game)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.SKIP.getCode());
        p.writeByte(0x01);
        return p;
    }

    public static Packet getMiniGameRequestTie(MiniGame game)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.REQUEST_TIE.getCode());
        return p;
    }

    public static Packet getMiniGameDenyTie(MiniGame game)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.ANSWER_TIE.getCode());
        return p;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="status">
    /// <para>1 = Room already closed</para>
    /// <para>2 = Can't enter due full cappacity</para>
    /// <para>3 = Other requests at this minute</para>
    /// <para>4 = Can't do while dead</para>
    /// <para>5 = Can't do while middle event</para>
    /// <para>6 = This character unable to do it</para>
    /// <para>7, 20 = Not allowed to trade anymore</para>
    /// <para>9 = Can only trade on same map</para>
    /// <para>10 = May not open store near portal</para>
    /// <para>11, 14 = Can't start game here</para>
    /// <para>12 = Can't open store at this channel</para>
    /// <para>13 = Can't estabilish miniroom</para>
    /// <para>15 = Stores only an the free market</para>
    /// <para>16 = Lists the rooms at FM (?)</para>
    /// <para>17 = You may not enter this store</para>
    /// <para>18 = Owner undergoing store maintenance</para>
    /// <para>19 = Unable to enter tournament room</para>
    /// <para>21 = Not enough mesos to enter</para>
    /// <para>22 = Incorrect password</para>
    /// </param>
    /// <returns></returns>
    public static Packet getMiniRoomError(int status)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.ROOM.getCode());
        p.writeByte(0);
        p.writeByte(status);
        return p;
    }

    public static Packet getMiniGameSkipVisitor(MiniGame game)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeShort(PlayerInteractionHandler.Action.SKIP.getCode());
        return p;
    }

    public static Packet getMiniGameMoveOmok(MiniGame game, int move1, int move2, int move3)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.MOVE_OMOK.getCode());
        p.writeInt(move1);
        p.writeInt(move2);
        p.writeByte(move3);
        return p;
    }

    public static Packet getMiniGameNewVisitor(MiniGame minigame, IPlayer chr, int slot)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.VISIT.getCode());
        p.writeByte(slot);
        addCharLook(p, chr, false);
        p.writeString(chr.getName());
        p.writeInt(1);
        p.writeInt(chr.getMiniGamePoints(MiniGameResult.WIN, true));
        p.writeInt(chr.getMiniGamePoints(MiniGameResult.TIE, true));
        p.writeInt(chr.getMiniGamePoints(MiniGameResult.LOSS, true));
        p.writeInt(minigame.getVisitorScore());
        return p;
    }

    public static Packet getMiniGameRemoveVisitor()
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.EXIT.getCode());
        p.writeByte(1);
        return p;
    }

    private static Packet getMiniGameResult(MiniGame game, int tie, int result, int forfeit)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.GET_RESULT.getCode());

        int matchResultType;
        if (tie == 0 && forfeit != 1)
        {
            matchResultType = 0;
        }
        else if (tie != 0)
        {
            matchResultType = 1;
        }
        else
        {
            matchResultType = 2;
        }

        p.writeByte(matchResultType);
        p.writeBool(result == 2); // host/visitor wins

        bool omok = game.isOmok();
        if (matchResultType == 1)
        {
            p.writeByte(0);
            p.writeShort(0);
            p.writeInt(game.getOwner().getMiniGamePoints(MiniGameResult.WIN, omok)); // wins
            p.writeInt(game.getOwner().getMiniGamePoints(MiniGameResult.TIE, omok)); // ties
            p.writeInt(game.getOwner().getMiniGamePoints(MiniGameResult.LOSS, omok)); // losses
            p.writeInt(game.getOwnerScore()); // points

            p.writeInt(0); // unknown
            p.writeInt(game.getVisitor().getMiniGamePoints(MiniGameResult.WIN, omok)); // wins
            p.writeInt(game.getVisitor().getMiniGamePoints(MiniGameResult.TIE, omok)); // ties
            p.writeInt(game.getVisitor().getMiniGamePoints(MiniGameResult.LOSS, omok)); // losses
            p.writeInt(game.getVisitorScore()); // points
            p.writeByte(0);
        }
        else
        {
            p.writeInt(0);
            p.writeInt(game.getOwner().getMiniGamePoints(MiniGameResult.WIN, omok)); // wins
            p.writeInt(game.getOwner().getMiniGamePoints(MiniGameResult.TIE, omok)); // ties
            p.writeInt(game.getOwner().getMiniGamePoints(MiniGameResult.LOSS, omok)); // losses
            p.writeInt(game.getOwnerScore()); // points
            p.writeInt(0);
            p.writeInt(game.getVisitor().getMiniGamePoints(MiniGameResult.WIN, omok)); // wins
            p.writeInt(game.getVisitor().getMiniGamePoints(MiniGameResult.TIE, omok)); // ties
            p.writeInt(game.getVisitor().getMiniGamePoints(MiniGameResult.LOSS, omok)); // losses
            p.writeInt(game.getVisitorScore()); // points
        }

        return p;
    }

    public static Packet getMiniGameOwnerWin(MiniGame game, bool forfeit)
    {
        return getMiniGameResult(game, 0, 1, forfeit ? 1 : 0);
    }

    public static Packet getMiniGameVisitorWin(MiniGame game, bool forfeit)
    {
        return getMiniGameResult(game, 0, 2, forfeit ? 1 : 0);
    }

    public static Packet getMiniGameTie(MiniGame game)
    {
        return getMiniGameResult(game, 1, 3, 0);
    }

    public static Packet getMiniGameClose(bool visitor, int type)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.EXIT.getCode());
        p.writeBool(visitor);
        p.writeByte(type); /* 2 : CRASH 3 : The room has been closed 4 : You have left the room 5 : You have been expelled  */
        return p;
    }

    public static Packet getMatchCard(IClient c, MiniGame minigame, bool owner, int piece)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.ROOM.getCode());
        p.writeByte(2);
        p.writeByte(2);
        p.writeBool(!owner);
        p.writeByte(0);
        addCharLook(p, minigame.getOwner(), false);
        p.writeString(minigame.getOwner().getName());
        if (minigame.getVisitor() != null)
        {
            IPlayer visitor = minigame.getVisitor();
            p.writeByte(1);
            addCharLook(p, visitor, false);
            p.writeString(visitor.getName());
        }
        p.writeByte(0xFF);
        p.writeByte(0);
        p.writeInt(2);
        p.writeInt(minigame.getOwner().getMiniGamePoints(MiniGameResult.WIN, false));
        p.writeInt(minigame.getOwner().getMiniGamePoints(MiniGameResult.TIE, false));
        p.writeInt(minigame.getOwner().getMiniGamePoints(MiniGameResult.LOSS, false));

        //set vs
        p.writeInt(minigame.getOwnerScore());
        if (minigame.getVisitor() != null)
        {
            IPlayer visitor = minigame.getVisitor();
            p.writeByte(1);
            p.writeInt(2);
            p.writeInt(visitor.getMiniGamePoints(MiniGameResult.WIN, false));
            p.writeInt(visitor.getMiniGamePoints(MiniGameResult.TIE, false));
            p.writeInt(visitor.getMiniGamePoints(MiniGameResult.LOSS, false));
            p.writeInt(minigame.getVisitorScore());
        }
        p.writeByte(0xFF);
        p.writeString(minigame.getDescription());
        p.writeByte(piece);
        p.writeByte(0);
        return p;
    }

    public static Packet getMatchCardStart(MiniGame game, int loser)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.START.getCode());
        p.writeByte(loser);

        int last;
        if (game.getMatchesToWin() > 10)
        {
            last = 30;
        }
        else if (game.getMatchesToWin() > 6)
        {
            last = 20;
        }
        else
        {
            last = 12;
        }

        p.writeByte(last);
        for (int i = 0; i < last; i++)
        {
            p.writeInt(game.getCardId(i));
        }
        return p;
    }

    public static Packet getMatchCardNewVisitor(MiniGame minigame, IPlayer chr, int slot)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.VISIT.getCode());
        p.writeByte(slot);
        addCharLook(p, chr, false);
        p.writeString(chr.getName());
        p.writeInt(1);
        p.writeInt(chr.getMiniGamePoints(MiniGameResult.WIN, false));
        p.writeInt(chr.getMiniGamePoints(MiniGameResult.TIE, false));
        p.writeInt(chr.getMiniGamePoints(MiniGameResult.LOSS, false));
        p.writeInt(minigame.getVisitorScore());
        return p;
    }

    public static Packet getMatchCardSelect(MiniGame game, int turn, int slot, int firstslot, int type)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.SELECT_CARD.getCode());
        p.writeByte(turn);
        if (turn == 1)
        {
            p.writeByte(slot);
        }
        else if (turn == 0)
        {
            p.writeByte(slot);
            p.writeByte(firstslot);
            p.writeByte(type);
        }
        return p;
    }

    // RPS_GAME packets thanks to Arnah (Vertisy)
    public static Packet openRPSNPC()
    {
        OutPacket p = OutPacket.create(SendOpcode.RPS_GAME);
        p.writeByte(8);// open npc
        p.writeInt(NpcId.RPS_ADMIN);
        return p;
    }

    public static Packet rpsMesoError(int mesos)
    {
        OutPacket p = OutPacket.create(SendOpcode.RPS_GAME);
        p.writeByte(0x06);
        if (mesos != -1)
        {
            p.writeInt(mesos);
        }
        return p;
    }

    public static Packet rpsSelection(byte selection, sbyte answer)
    {
        OutPacket p = OutPacket.create(SendOpcode.RPS_GAME);
        p.writeByte(0x0B);// 11l
        p.writeByte(selection);
        p.writeSByte(answer);
        return p;
    }

    public static Packet rpsMode(byte mode)
    {
        OutPacket p = OutPacket.create(SendOpcode.RPS_GAME);
        p.writeByte(mode);
        return p;
    }

    public static Packet fredrickMessage(byte operation)
    {
        OutPacket p = OutPacket.create(SendOpcode.FREDRICK_MESSAGE);
        p.writeByte(operation);
        return p;
    }

    public static Packet getFredrick(byte op)
    {
        OutPacket p = OutPacket.create(SendOpcode.FREDRICK);
        p.writeByte(op);

        switch (op)
        {
            case 0x24:
                p.skip(8);
                break;
            default:
                p.writeByte(0);
                break;
        }

        return p;
    }

    public static Packet getFredrick(IPlayer chr)
    {
        OutPacket p = OutPacket.create(SendOpcode.FREDRICK);
        p.writeByte(0x23);
        p.writeInt(NpcId.FREDRICK);
        p.writeInt(32272); //id
        p.skip(5);
        p.writeInt(chr.getMerchantNetMeso());
        p.writeByte(0);
        try
        {
            var items = ItemFactory.MERCHANT.loadItems(chr.getId(), false);
            p.writeByte(items.Count);

            foreach (var item in items)
            {
                addItemInfo(p, item.Item, true);
            }
        }
        catch (Exception e)
        {
            Log.Logger.Error(e.ToString());
        }
        p.skip(3);
        return p;
    }

    public static Packet AddMiniGameBox(IPlayer chr, int amount, int type)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_CHAR_BOX);
        p.writeInt(chr.getId());
        addAnnounceBox(p, chr.getMiniGame()!, amount, type);
        return p;
    }

    public static Packet addOmokBox(IPlayer chr, int amount, int type)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_CHAR_BOX);
        p.writeInt(chr.getId());
        addAnnounceBox(p, chr.getMiniGame()!, amount, type);
        return p;
    }

    public static Packet addMatchCardBox(IPlayer chr, int amount, int type)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_CHAR_BOX);
        p.writeInt(chr.getId());
        addAnnounceBox(p, chr.getMiniGame()!, amount, type);
        return p;
    }

    public static Packet removeMinigameBox(IPlayer chr)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_CHAR_BOX);
        p.writeInt(chr.getId());
        p.writeByte(0);
        return p;
    }

    public static Packet getPlayerShopChat(IPlayer chr, string chat, byte slot)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.CHAT.getCode());
        p.writeByte(PlayerInteractionHandler.Action.CHAT_THING.getCode());
        p.writeByte(slot);
        p.writeString(chr.getName() + " : " + chat);
        return p;
    }

    public static Packet getTradeChat(IPlayer chr, string chat, bool owner)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.CHAT.getCode());
        p.writeByte(PlayerInteractionHandler.Action.CHAT_THING.getCode());
        p.writeByte(owner ? 0 : 1);
        p.writeString(chr.getName() + " : " + chat);
        return p;
    }

    public static Packet hiredMerchantBox()
    {
        OutPacket p = OutPacket.create(SendOpcode.ENTRUSTED_SHOP_CHECK_RESULT); // header.
        p.writeByte(0x07);
        return p;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="msg">
    /// <para>0: Success</para>
    /// <para>1: The room is already closed.</para>
    /// <para>2: You can't enter the room due to full capacity.</para>
    /// <para>3: Other requests are being fulfilled this minute.</para>
    /// <para>4: You can't do it while you're dead.</para>
    /// <para>7: You are not allowed to trade other items at this point.</para>
    /// <para>17: You may not enter this store.</para>
    /// <para>18: The owner of the store is currently undergoing store maintenance. Please try again in a bit.</para>
    /// <para>23: This can only be used inside the Free Market.</para>
    /// <para>default: This character is unable to do it.</para>
    /// </param>
    /// <returns></returns>
    public static Packet getOwlMessage(int msg)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOP_LINK_RESULT);
        p.writeByte(msg); // depending on the byte sent, a different message is sent.
        return p;
    }

    public static Packet owlOfMinerva(IClient c, int itemId, List<KeyValuePair<PlayerShopItem, AbstractMapObject>> hmsAvailable)
    {
        sbyte itemType = ItemConstants.getInventoryType(itemId).getType();

        OutPacket p = OutPacket.create(SendOpcode.SHOP_SCANNER_RESULT);
        p.writeByte(6);
        p.writeInt(0);
        p.writeInt(itemId);
        p.writeInt(hmsAvailable.Count);
        foreach (var hme in hmsAvailable)
        {
            var item = hme.Key;
            var mo = hme.Value;

            if (mo is PlayerShop ps)
            {
                IPlayer owner = ps.getOwner();

                p.writeString(owner.getName());
                p.writeInt(owner.getMapId());
                p.writeString(ps.getDescription());
                p.writeInt(item.getBundles());
                p.writeInt(item.getItem().getQuantity());
                p.writeInt(item.getPrice());
                p.writeInt(owner.getId());
                p.writeByte(owner.getClient().getChannel() - 1);
            }
            else
            {
                HiredMerchant hm = (HiredMerchant)mo;

                p.writeString(hm.getOwner());
                p.writeInt(hm.getMapId());
                p.writeString(hm.getDescription());
                p.writeInt(item.getBundles());
                p.writeInt(item.getItem().getQuantity());
                p.writeInt(item.getPrice());
                p.writeInt(hm.getOwnerId());
                p.writeByte(hm.getChannel() - 1);
            }

            p.writeByte(itemType);
            if (itemType == InventoryType.EQUIP.getType())
            {
                addItemInfo(p, item.getItem(), true);
            }
        }
        return p;
    }

    public static Packet getOwlOpen(List<int> owlLeaderboards)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOP_SCANNER_RESULT);
        p.writeByte(7);
        p.writeByte(owlLeaderboards.Count);
        foreach (int i in owlLeaderboards)
        {
            p.writeInt(i);
        }

        return p;
    }

    public static Packet retrieveFirstMessage()
    {
        OutPacket p = OutPacket.create(SendOpcode.ENTRUSTED_SHOP_CHECK_RESULT); // header.
        p.writeByte(0x09);
        return p;
    }

    public static Packet remoteChannelChange(byte ch)
    {
        OutPacket p = OutPacket.create(SendOpcode.ENTRUSTED_SHOP_CHECK_RESULT); // header.
        p.writeByte(0x10);
        p.writeInt(0);//No idea yet
        p.writeByte(ch);
        return p;
    }
    /*
     * Possible things for ENTRUSTED_SHOP_CHECK_RESULT
     * 0x0E = 00 = Renaming Failed - Can't find the merchant, 01 = Renaming successful
     * 0x10 = Changes channel to the store (Store is open at Channel 1, do you want to change channels?)
     * 0x11 = You cannot sell any items when managing.. blabla
     * 0x12 = FKING POPUP LOL
     */
    public static Packet getHiredMerchant(IPlayer chr, HiredMerchant hm, bool firstTime)
    {
        //Thanks Dustin
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.ROOM.getCode());
        p.writeByte(0x05);
        p.writeByte(0x04);
        p.writeShort(hm.getVisitorSlotThreadsafe(chr) + 1);
        p.writeInt(hm.getItemId());
        p.writeString("Hired Merchant");

        IPlayer[] visitors = hm.getVisitorCharacters();
        for (int i = 0; i < 3; i++)
        {
            if (visitors[i] != null)
            {
                p.writeByte(i + 1);
                addCharLook(p, visitors[i], false);
                p.writeString(visitors[i].getName());
            }
        }
        p.writeByte(-1);
        if (hm.isOwner(chr))
        {
            var msgList = hm.getMessages();

            p.writeShort(msgList.Count);
            foreach (var stringBytePair in msgList)
            {
                p.writeString(stringBytePair.Key);
                p.writeByte(stringBytePair.Value);
            }
        }
        else
        {
            p.writeShort(0);
        }
        p.writeString(hm.getOwner());
        if (hm.isOwner(chr))
        {
            p.writeShort(0);
            p.writeShort(hm.getTimeOpen());
            p.writeByte(firstTime ? 1 : 0);
            var sold = hm.getSold();
            p.writeByte(sold.Count);
            foreach (var s in sold)
            {
                p.writeInt(s.itemid);
                p.writeShort(s.quantity);
                p.writeInt(s.mesos);
                p.writeString(s.buyer);
            }
            p.writeInt(chr.getMerchantMeso());//:D?
        }
        p.writeString(hm.getDescription());
        p.writeByte(0x10); //TODO SLOTS, which is 16 for most stores...slotMax
        p.writeInt(hm.isOwner(chr) ? chr.getMerchantMeso() : chr.getMeso());
        p.writeByte(hm.getItems().Count);
        if (hm.getItems().Count == 0)
        {
            p.writeByte(0);//Hmm??
        }
        else
        {
            foreach (PlayerShopItem item in hm.getItems())
            {
                p.writeShort(item.getBundles());
                p.writeShort(item.getItem().getQuantity());
                p.writeInt(item.getPrice());
                addItemInfo(p, item.getItem(), true);
            }
        }
        return p;
    }

    public static Packet updateHiredMerchant(HiredMerchant hm, IPlayer chr)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.UPDATE_MERCHANT.getCode());
        p.writeInt(hm.isOwner(chr) ? chr.getMerchantMeso() : chr.getMeso());
        p.writeByte(hm.getItems().Count);
        foreach (PlayerShopItem item in hm.getItems())
        {
            p.writeShort(item.getBundles());
            p.writeShort(item.getItem().getQuantity());
            p.writeInt(item.getPrice());
            addItemInfo(p, item.getItem(), true);
        }
        return p;
    }

    public static Packet hiredMerchantChat(string message, byte slot)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.CHAT.getCode());
        p.writeByte(PlayerInteractionHandler.Action.CHAT_THING.getCode());
        p.writeByte(slot);
        p.writeString(message);
        return p;
    }

    public static Packet hiredMerchantVisitorLeave(int slot)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.EXIT.getCode());
        if (slot != 0)
        {
            p.writeByte(slot);
        }
        return p;
    }

    public static Packet hiredMerchantOwnerLeave()
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.REAL_CLOSE_MERCHANT.getCode());
        p.writeByte(0);
        return p;
    }

    public static Packet hiredMerchantOwnerMaintenanceLeave()
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.REAL_CLOSE_MERCHANT.getCode());
        p.writeByte(5);
        return p;
    }

    public static Packet hiredMerchantMaintenanceMessage()
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.ROOM.getCode());
        p.writeByte(0x00);
        p.writeByte(0x12);
        return p;
    }

    public static Packet leaveHiredMerchant(int slot, int status2)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.EXIT.getCode());
        p.writeByte(slot);
        p.writeByte(status2);
        return p;
    }

    /**
     * @param pastVisitors Merchant visitors. The first 10 names will be shown,
     *                     everything beyond will layered over each other at the top of the window.
     */
    public static Packet viewMerchantVisitorHistory(List<PastVisitor> pastVisitors)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.VIEW_VISITORS.getCode());
        p.writeShort(pastVisitors.Count);
        foreach (PastVisitor pastVisitor in pastVisitors)
        {
            p.writeString(pastVisitor.chrName);
            p.writeInt((int)pastVisitor.visitDuration.TotalMilliseconds); // milliseconds, displayed as hours and minutes
        }
        return p;
    }

    /**
     * @param chrNames Blacklisted names. The first 20 names will be displayed, anything beyond does no difference.
     */
    public static Packet viewMerchantBlacklist(HashSet<string> chrNames)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.VIEW_BLACKLIST.getCode());
        p.writeShort(chrNames.Count);
        foreach (string chrName in chrNames)
        {
            p.writeString(chrName);
        }
        return p;
    }

    public static Packet hiredMerchantVisitorAdd(IPlayer chr, int slot)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(PlayerInteractionHandler.Action.VISIT.getCode());
        p.writeByte(slot);
        addCharLook(p, chr, false);
        p.writeString(chr.getName());
        return p;
    }

    public static Packet spawnHiredMerchantBox(HiredMerchant hm)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_HIRED_MERCHANT);
        p.writeInt(hm.getOwnerId());
        p.writeInt(hm.getItemId());
        p.writeShort((short)hm.getPosition().X);
        p.writeShort((short)hm.getPosition().Y);
        p.writeShort(0);
        p.writeString(hm.getOwner());
        p.writeByte(0x05);
        p.writeInt(hm.getObjectId());
        p.writeString(hm.getDescription());
        p.writeByte(hm.getItemId() % 100);
        p.writeBytes(new byte[] { 1, 4 });
        return p;
    }

    public static Packet removeHiredMerchantBox(int id)
    {
        OutPacket p = OutPacket.create(SendOpcode.DESTROY_HIRED_MERCHANT);
        p.writeInt(id);
        return p;
    }

    public static Packet spawnPlayerNPC(PlayerNPC npc)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_NPC_REQUEST_CONTROLLER);
        p.writeByte(1);
        p.writeInt(npc.getObjectId());
        p.writeInt(npc.getScriptId());
        p.writeShort(npc.getPosition().X);
        p.writeShort(npc.getCY());
        p.writeByte(npc.getDirection());
        p.writeShort(npc.getFH());
        p.writeShort(npc.getRX0());
        p.writeShort(npc.getRX1());
        p.writeByte(1);
        return p;
    }

    public static Packet getPlayerNPC(PlayerNPC npc)
    {     // thanks to Arnah
        OutPacket p = OutPacket.create(SendOpcode.IMITATED_NPC_DATA);
        p.writeByte(0x01);
        p.writeInt(npc.getScriptId());
        p.writeString(npc.getName());
        p.writeByte(npc.getGender());
        p.writeByte(npc.getSkin());
        p.writeInt(npc.getFace());
        p.writeByte(0);
        p.writeInt(npc.getHair());
        Dictionary<short, int> equip = npc.getEquips();
        Dictionary<short, int> myEquip = new();
        Dictionary<short, int> maskedEquip = new();
        foreach (short position in equip.Keys)
        {
            short pos = (byte)(position * -1);
            if (pos < 100 && myEquip.get(pos) == null)
            {
                myEquip.AddOrUpdate(pos, equip.GetValueOrDefault(position));
            }
            else if ((pos > 100 && pos != 111) || pos == -128)
            { // don't ask. o.o
                pos -= 100;
                if (myEquip.get(pos) != null)
                {
                    maskedEquip.AddOrUpdate(pos, myEquip.GetValueOrDefault(pos));
                }
                myEquip.AddOrUpdate(pos, equip.GetValueOrDefault(position));
            }
            else if (myEquip.get(pos) != null)
            {
                maskedEquip.AddOrUpdate(pos, equip.GetValueOrDefault(position));
            }
        }
        foreach (var entry in myEquip)
        {
            p.writeByte(entry.Key);
            p.writeInt(entry.Value);
        }
        p.writeByte(0xFF);
        foreach (var entry in maskedEquip)
        {
            p.writeByte(entry.Key);
            p.writeInt(entry.Value);
        }
        p.writeByte(0xFF);
        p.writeInt(equip.get((short)-111) ?? 0);
        for (int i = 0; i < 3; i++)
        {
            p.writeInt(0);
        }
        return p;
    }

    public static Packet removePlayerNPC(int oid)
    {
        OutPacket p = OutPacket.create(SendOpcode.IMITATED_NPC_DATA);
        p.writeByte(0x00);
        p.writeInt(oid);
        return p;
    }

    public static Packet sendYellowTip(string tip)
    {
        OutPacket p = OutPacket.create(SendOpcode.SET_WEEK_EVENT_MESSAGE);
        p.writeByte(0xFF);
        p.writeString(tip);
        p.writeShort(0);
        return p;
    }

    public static Packet givePirateBuff(IEnumerable<BuffStatValue> statups, int buffid, int duration)
    {
        OutPacket p = OutPacket.create(SendOpcode.GIVE_BUFF);
        bool infusion = buffid == Buccaneer.SPEED_INFUSION || buffid == ThunderBreaker.SPEED_INFUSION || buffid == Corsair.SPEED_INFUSION;
        writeLongMask(p, statups.ToArray());
        p.writeShort(0);
        foreach (var stat in statups)
        {
            p.writeInt(stat.Value);
            p.writeInt(buffid);
            p.skip(infusion ? 10 : 5);
            p.writeShort(duration);
        }
        p.skip(3);
        return p;
    }

    public static Packet giveForeignPirateBuff(int cid, int buffid, int time, params BuffStatValue[] statups)
    {
        OutPacket p = OutPacket.create(SendOpcode.GIVE_FOREIGN_BUFF);
        bool infusion = buffid == Buccaneer.SPEED_INFUSION || buffid == ThunderBreaker.SPEED_INFUSION || buffid == Corsair.SPEED_INFUSION;
        p.writeInt(cid);
        writeLongMask(p, statups);
        p.writeShort(0);
        foreach (var statup in statups)
        {
            p.writeInt(statup.Value);
            p.writeInt(buffid);
            p.skip(infusion ? 10 : 5);
            p.writeShort(time);
        }
        p.writeShort(0);
        p.writeByte(2);
        return p;
    }

    public static Packet sendMTS(List<MTSItemInfo> items, int tab, int type, int page, int pages)
    {
        OutPacket p = OutPacket.create(SendOpcode.MTS_OPERATION);
        p.writeByte(0x15); //operation
        p.writeInt(pages * 16); //testing, change to 10 if fails
        p.writeInt(items.Count); //number of items
        p.writeInt(tab);
        p.writeInt(type);
        p.writeInt(page);
        p.writeByte(1);
        p.writeByte(1);
        foreach (MTSItemInfo item in items)
        {
            addItemInfo(p, item.getItem(), true);
            p.writeInt(item.getID()); //id
            p.writeInt(item.getTaxes()); //this + below = price
            p.writeInt(item.getPrice()); //price
            p.writeInt(0);
            p.writeLong(getTime(item.getEndingDate()));
            p.writeString(item.getSeller()); //account name (what was nexon thinking?)
            p.writeString(item.getSeller()); //char name
            for (int j = 0; j < 28; j++)
            {
                p.writeByte(0);
            }
        }
        p.writeByte(1);
        return p;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="error">
    /// <para>0 = Player online, use whisper</para>
    /// <para>1 = Check player's name</para>
    /// <para>2 = Receiver inbox full</para>
    /// </param>
    /// <returns></returns>
    public static Packet noteError(byte error)
    {
        OutPacket p = OutPacket.create(SendOpcode.MEMO_RESULT);
        p.writeByte(5);
        p.writeByte(error);
        return p;
    }

    public static Packet useChalkboard(IPlayer chr, bool close)
    {
        OutPacket p = OutPacket.create(SendOpcode.CHALKBOARD);
        p.writeInt(chr.getId());
        if (close)
        {
            p.writeByte(0);
        }
        else
        {
            p.writeByte(1);
            p.writeString(chr.getChalkboard()!);
        }
        return p;
    }

    public static Packet trockRefreshMapList(IPlayer chr, bool delete, bool vip)
    {
        OutPacket p = OutPacket.create(SendOpcode.MAP_TRANSFER_RESULT);
        p.writeByte(delete ? 2 : 3);
        if (vip)
        {
            p.writeByte(1);
            List<int> map = chr.getVipTrockMaps();
            for (int i = 0; i < 10; i++)
            {
                p.writeInt(map[i]);
            }
        }
        else
        {
            p.writeByte(0);
            List<int> map = chr.getTrockMaps();
            for (int i = 0; i < 5; i++)
            {
                p.writeInt(map[i]);
            }
        }
        return p;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="error">
    /// <para>1: cannot find char info,</para>
    /// <para>2: cannot transfer under 20,</para>
    /// <para>3: cannot send banned,</para>
    /// <para>4: cannot send married,</para>
    /// <para>5: cannot send guild leader,</para>
    /// <para>6: cannot send if account already requested transfer,</para>
    /// <para>7: cannot transfer within 30days,</para>
    /// <para>8: must quit family,</para>
    /// <para>9: unknown error</para>
    /// </param>
    /// <param name="c"></param>
    /// <returns></returns>
    public static Packet sendWorldTransferRules(int error, IClient c)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_CHECK_TRANSFER_WORLD_POSSIBLE_RESULT);
        p.writeInt(0); //ignored
        p.writeByte(error);
        p.writeInt(0);
        p.writeBool(error == 0); //0 = ?, otherwise list servers
        if (error == 0)
        {
            var worlds = Server.getInstance().getWorlds();
            p.writeInt(worlds.Count);
            foreach (var world in worlds)
            {
                p.writeString(GameConstants.WORLD_NAMES[world.getId()]);
            }
        }
        return p;
    }

    public static Packet showWorldTransferSuccess(Item item, int accountId)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);
        p.writeByte(0xA0);
        addCashItemInformation(p, item, accountId);
        return p;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="error">
    /// <para>0: no error, send rules</para>
    /// <para>1: name change already submitted</para>
    /// <para>2: name change within a month</para>
    /// <para>3: recently banned</para>
    /// <para>4: unknown error</para>
    /// </param>
    /// <returns></returns>
    public static Packet sendNameTransferRules(int error)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_CHECK_NAME_CHANGE_POSSIBLE_RESULT);
        p.writeInt(0);
        p.writeByte(error);
        p.writeInt(0);

        return p;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="availableName">
    /// <para> 0: Name available</para>
    /// <para> &gt; 0: Name is in use</para>
    /// <para> &lt; 0: Unknown error</para>
    /// </param>
    /// <param name="canUseName"></param>
    /// <returns></returns>
    public static Packet sendNameTransferCheck(string availableName, bool canUseName)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_CHECK_NAME_CHANGE);
        //Send provided name back to client to add to temporary cache of checked & accepted names
        p.writeString(availableName);
        p.writeBool(!canUseName);
        return p;
    }

    public static Packet showNameChangeSuccess(Item item, int accountId)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);
        p.writeByte(0x9E);
        addCashItemInformation(p, item, accountId);
        return p;
    }

    public static Packet showNameChangeCancel(bool success)
    {
        OutPacket p = OutPacket.create(SendOpcode.CANCEL_NAME_CHANGE_RESULT);
        p.writeBool(success);
        if (!success)
        {
            p.writeByte(0);
        }
        //p.writeString("Custom message."); //only if ^ != 0
        return p;
    }

    public static Packet showWorldTransferCancel(bool success)
    {
        OutPacket p = OutPacket.create(SendOpcode.CANCEL_TRANSFER_WORLD_RESULT);
        p.writeBool(success);
        if (!success)
        {
            p.writeByte(0);
        }
        //p.writeString("Custom message."); //only if ^ != 0
        return p;
    }

    public static Packet showMTSCash(IPlayer chr)
    {
        OutPacket p = OutPacket.create(SendOpcode.MTS_OPERATION2);
        p.writeInt(chr.getCashShop().getCash(CashShop.NX_PREPAID));
        p.writeInt(chr.getCashShop().getCash(CashShop.MAPLE_POINT));
        return p;
    }

    public static Packet MTSWantedListingOver(int nx, int items)
    {
        OutPacket p = OutPacket.create(SendOpcode.MTS_OPERATION);
        p.writeByte(0x3D);
        p.writeInt(nx);
        p.writeInt(items);
        return p;
    }

    public static Packet MTSConfirmSell()
    {
        OutPacket p = OutPacket.create(SendOpcode.MTS_OPERATION);
        p.writeByte(0x1D);
        return p;
    }

    public static Packet MTSConfirmBuy()
    {
        OutPacket p = OutPacket.create(SendOpcode.MTS_OPERATION);
        p.writeByte(0x33);
        return p;
    }

    public static Packet MTSFailBuy()
    {
        OutPacket p = OutPacket.create(SendOpcode.MTS_OPERATION);
        p.writeByte(0x34);
        p.writeByte(0x42);
        return p;
    }

    public static Packet MTSConfirmTransfer(int quantity, int pos)
    {
        OutPacket p = OutPacket.create(SendOpcode.MTS_OPERATION);
        p.writeByte(0x27);
        p.writeInt(quantity);
        p.writeInt(pos);
        return p;
    }

    public static Packet notYetSoldInv(List<MTSItemInfo> items)
    {
        OutPacket p = OutPacket.create(SendOpcode.MTS_OPERATION);
        p.writeByte(0x23);
        p.writeInt(items.Count);
        if (items.Count > 0)
        {
            foreach (MTSItemInfo item in items)
            {
                addItemInfo(p, item.getItem(), true);
                p.writeInt(item.getID()); //id
                p.writeInt(item.getTaxes()); //this + below = price
                p.writeInt(item.getPrice()); //price
                p.writeInt(0);
                p.writeLong(getTime(item.getEndingDate()));
                p.writeString(item.getSeller()); //account name (what was nexon thinking?)
                p.writeString(item.getSeller()); //char name
                for (int i = 0; i < 28; i++)
                {
                    p.writeByte(0);
                }
            }
        }
        else
        {
            p.writeInt(0);
        }
        return p;
    }

    public static Packet transferInventory(List<MTSItemInfo> items)
    {
        OutPacket p = OutPacket.create(SendOpcode.MTS_OPERATION);
        p.writeByte(0x21);
        p.writeInt(items.Count);
        if (items.Count > 0)
        {
            foreach (MTSItemInfo item in items)
            {
                addItemInfo(p, item.getItem(), true);
                p.writeInt(item.getID()); //id
                p.writeInt(item.getTaxes()); //taxes
                p.writeInt(item.getPrice()); //price
                p.writeInt(0);
                p.writeLong(getTime(item.getEndingDate()));
                p.writeString(item.getSeller()); //account name (what was nexon thinking?)
                p.writeString(item.getSeller()); //char name
                for (int i = 0; i < 28; i++)
                {
                    p.writeByte(0);
                }
            }
        }
        p.writeByte(0xD0 + items.Count);
        p.writeSBytes(new sbyte[] { -1, -1, -1, 0 });
        return p;
    }

    public static Packet showCouponRedeemedItems(int accountId, int maplePoints, int mesos, List<Item> cashItems, List<ItemQuantity> items)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);
        p.writeByte(0x59);
        p.writeByte((byte)cashItems.Count);
        foreach (Item item in cashItems)
        {
            addCashItemInformation(p, item, accountId);
        }
        p.writeInt(maplePoints);
        p.writeInt(items.Count);
        foreach (var itemPair in items)
        {
            int quantity = itemPair.Quantity;
            p.writeShort((short)quantity); //quantity (0 = 1 for cash items)
            p.writeShort(0x1F); //0 = ?, >=0x20 = ?, <0x20 = ? (does nothing?)
            p.writeInt(itemPair.ItemId);
        }
        p.writeInt(mesos);
        return p;
    }

    public static Packet showCash(IPlayer mc)
    {
        OutPacket p = OutPacket.create(SendOpcode.QUERY_CASH_RESULT);
        p.writeInt(mc.getCashShop().getCash(CashShop.NX_CREDIT));
        p.writeInt(mc.getCashShop().getCash(CashShop.MAPLE_POINT));
        p.writeInt(mc.getCashShop().getCash(CashShop.NX_PREPAID));
        return p;
    }

    public static Packet enableCSUse(IPlayer mc)
    {
        return showCash(mc);
    }

    public static class WhisperFlag
    {
        public const byte LOCATION = 0x01;
        public const byte WHISPER = 0x02;
        public const byte REQUEST = 0x04;
        public const byte RESULT = 0x08;
        public const byte RECEIVE = 0x10;
        public const byte BLOCKED = 0x20;
        public const byte LOCATION_FRIEND = 0x40;
    }

    /**
     * User for /find, buddy find and /c (chase)
     * CField::OnWhisper
     *
     * @param target         Name string from the command parameter
     * @param type           Location of the target
     * @param fieldOrChannel If true & chr is not null, shows different channel message
     * @param flag           LOCATION or LOCATION_FRIEND
     * @return packet structure
     */
    public static Packet getFindResult(IPlayer target, byte type, int fieldOrChannel, byte flag)
    {
        OutPacket p = OutPacket.create(SendOpcode.WHISPER);

        p.writeByte(flag | WhisperFlag.RESULT);
        p.writeString(target.getName());
        p.writeByte(type);
        p.writeInt(fieldOrChannel);

        if (type == WhisperHandler.RT_SAME_CHANNEL)
        {
            p.writeInt(target.getPosition().X);
            p.writeInt(target.getPosition().Y);
        }

        return p;
    }

    public static Packet getWhisperResult(string target, bool success)
    {
        OutPacket p = OutPacket.create(SendOpcode.WHISPER);
        p.writeByte(WhisperFlag.WHISPER | WhisperFlag.RESULT);
        p.writeString(target);
        p.writeBool(success);
        return p;
    }

    public static Packet getWhisperReceive(string sender, int channel, bool fromAdmin, string message)
    {
        OutPacket p = OutPacket.create(SendOpcode.WHISPER);
        p.writeByte(WhisperFlag.WHISPER | WhisperFlag.RECEIVE);
        p.writeString(sender);
        p.writeByte(channel);
        p.writeBool(fromAdmin);
        p.writeString(message);
        return p;
    }

    public static Packet sendAutoHpPot(int itemId)
    {
        OutPacket p = OutPacket.create(SendOpcode.AUTO_HP_POT);
        p.writeInt(itemId);
        return p;
    }

    public static Packet sendAutoMpPot(int itemId)
    {
        OutPacket p = OutPacket.create(SendOpcode.AUTO_MP_POT);
        p.writeInt(itemId);
        return p;
    }

    public static Packet showOXQuiz(int questionSet, int questionId, bool askQuestion)
    {
        OutPacket p = OutPacket.create(SendOpcode.OX_QUIZ);
        p.writeByte(askQuestion ? 1 : 0);
        p.writeByte(questionSet);
        p.writeShort(questionId);
        return p;
    }

    public static Packet updateGender(IPlayer chr)
    {
        OutPacket p = OutPacket.create(SendOpcode.SET_GENDER);
        p.writeByte(chr.getGender());
        return p;
    }

    public static Packet enableReport()
    { 
        // thanks to snow
        OutPacket p = OutPacket.create(SendOpcode.CLAIM_STATUS_CHANGED);
        p.writeByte(1);
        return p;
    }

    public static Packet giveFinalAttack(int skillid, int time)
    { 
        // packets found thanks to lailainoob
        OutPacket p = OutPacket.create(SendOpcode.GIVE_BUFF);
        p.writeLong(0);
        p.writeShort(0);
        p.writeByte(0);//some 80 and 0 bs DIRECTION
        p.writeByte(0x80);//let's just do 80, then 0
        p.writeInt(0);
        p.writeShort(1);
        p.writeInt(skillid);
        p.writeInt(time);
        p.writeInt(0);
        return p;
    }

    public static Packet loadFamily(IPlayer player)
    {
        OutPacket p = OutPacket.create(SendOpcode.FAMILY_PRIVILEGE_LIST);
        p.writeInt(FamilyEntitlement.values<FamilyEntitlement>().Length);
        for (int i = 0; i < FamilyEntitlement.values<FamilyEntitlement>().Length; i++)
        {
            FamilyEntitlement entitlement = FamilyEntitlement.values<FamilyEntitlement>()[i];
            p.writeByte(i <= 1 ? 1 : 2); //type
            p.writeInt(entitlement.getRepCost());
            p.writeInt(entitlement.getUsageLimit());
            p.writeString(entitlement.getName());
            p.writeString(entitlement.getDescription());
        }
        return p;
    }

    /**
     * Family Result Message
     * <p>
     * Possible values for <code>type</code>:<br>
     * 64: You cannot add this character as a junior.
     * 65: The name could not be found or is not online.
     * 66: You belong to the same family.
     * 67: You do not belong to the same family.<br>
     * 69: The character you wish to add as\r\na Junior must be in the same
     * map.<br>
     * 70: This character is already a Junior of another character.<br>
     * 71: The Junior you wish to add\r\nmust be at a lower rank.<br>
     * 72: The gap between you and your\r\njunior must be within 20 levels.<br>
     * 73: Another character has requested to add this character.\r\nPlease try
     * again later.<br>
     * 74: Another character has requested a summon.\r\nPlease try again
     * later.<br>
     * 75: The summons has failed. Your current location or state does not allow
     * a summons.<br>
     * 76: The family cannot extend more than 1000 generations from above and
     * below.<br>
     * 77: The Junior you wish to add\r\nmust be over Level 10.<br>
     * 78: You cannot add a Junior \r\nthat has requested to change worlds.<br>
     * 79: You cannot add a Junior \r\nsince you've requested to change
     * worlds.<br>
     * 80: Separation is not possible due to insufficient Mesos.\r\nYou will
     * need %d Mesos to\r\nseparate with a Senior.<br>
     * 81: Separation is not possible due to insufficient Mesos.\r\nYou will
     * need %d Mesos to\r\nseparate with a Junior.<br>
     * 82: The Entitlement does not apply because your level does not match the
     * corresponding area.<br>
     *
     * @param type The type
     * @return Family Result packet
     */
    public static Packet sendFamilyMessage(int type, int mesos)
    {
        OutPacket p = OutPacket.create(SendOpcode.FAMILY_RESULT);
        p.writeInt(type);
        p.writeInt(mesos);
        return p;
    }

    public static Packet getFamilyInfo(FamilyEntry? f)
    {
        if (f == null)
        {
            return getEmptyFamilyInfo();
        }

        OutPacket p = OutPacket.create(SendOpcode.FAMILY_INFO_RESULT);
        p.writeInt(f.getReputation()); // cur rep left
        p.writeInt(f.getTotalReputation()); // tot rep left
        p.writeInt(f.getTodaysRep()); // todays rep
        p.writeShort(f.getJuniorCount()); // juniors added
        p.writeShort(2); // juniors allowed
        p.writeShort(0); //Unknown
        p.writeInt(f.getFamily().getLeader().getChrId()); // Leader ID (Allows setting message)
        p.writeString(f.getFamily().getName());
        p.writeString(f.getFamily().getMessage()); //family message
        p.writeInt(FamilyEntitlement.values<FamilyEntitlement>().Length); //Entitlement info count
        foreach (FamilyEntitlement entitlement in FamilyEntitlement.values<FamilyEntitlement>())
        {
            p.writeInt(entitlement.ordinal()); //ID
            p.writeInt(f.isEntitlementUsed(entitlement) ? 1 : 0); //Used count
        }
        return p;
    }

    private static Packet getEmptyFamilyInfo()
    {
        OutPacket p = OutPacket.create(SendOpcode.FAMILY_INFO_RESULT);
        p.writeInt(0); // cur rep left
        p.writeInt(0); // tot rep left
        p.writeInt(0); // todays rep
        p.writeShort(0); // juniors added
        p.writeShort(2); // juniors allowed
        p.writeShort(0); //Unknown
        p.writeInt(0); // Leader ID (Allows setting message)
        p.writeString("");
        p.writeString(""); //family message
        p.writeInt(0);
        return p;
    }

    public static Packet showPedigree(FamilyEntry entry)
    {
        OutPacket p = OutPacket.create(SendOpcode.FAMILY_CHART_RESULT);
        p.writeInt(entry.getChrId()); //ID of viewed player's pedigree, can't be leader?
        List<FamilyEntry> superJuniors = new(4);
        bool hasOtherJunior = false;
        int entryCount = 2; //2 guaranteed, leader and self
        entryCount += Math.Min(2, entry.getTotalSeniors());
        //needed since OutPacket doesn't have any seek functionality
        if (entry.getSenior() != null)
        {
            if (entry.getSenior()!.getJuniorCount() == 2)
            {
                entryCount++;
                hasOtherJunior = true;
            }
        }
        foreach (var junior in entry.getJuniors())
        {
            if (junior == null)
            {
                continue;
            }
            entryCount++;
            foreach (var superJunior in junior.getJuniors())
            {
                if (superJunior == null)
                {
                    continue;
                }
                entryCount++;
                superJuniors.Add(superJunior);
            }
        }
        //write entries
        bool missingEntries = entryCount == 2; //pedigree requires at least 3 entries to show leader, might only have 2 if leader's juniors leave
        if (missingEntries)
        {
            entryCount++;
        }
        p.writeInt(entryCount); //player count
        addPedigreeEntry(p, entry.getFamily().getLeader());
        if (entry.getSenior() != null)
        {
            if (entry.getSenior()!.getSenior() != null)
            {
                addPedigreeEntry(p, entry.getSenior()!.getSenior()!);
            }
            addPedigreeEntry(p, entry.getSenior()!);
        }
        addPedigreeEntry(p, entry);
        if (hasOtherJunior)
        { 
            //must be sent after own entry
            var otherJunior = entry.getSenior()?.getOtherJunior(entry);
            if (otherJunior != null)
            {
                addPedigreeEntry(p, otherJunior);
            }
        }
        if (missingEntries)
        {
            addPedigreeEntry(p, entry);
        }
        foreach (var junior in entry.getJuniors())
        {
            if (junior == null)
            {
                continue;
            }
            addPedigreeEntry(p, junior);
            foreach (var superJunior in junior.getJuniors())
            {
                if (superJunior != null)
                {
                    addPedigreeEntry(p, superJunior);
                }
            }
        }
        p.writeInt(2 + superJuniors.Count); //member info count
                                            // 0 = total seniors, -1 = total members, otherwise junior count of ID
        p.writeInt(-1);
        p.writeInt(entry.getFamily().getTotalMembers());
        p.writeInt(0);
        p.writeInt(entry.getTotalSeniors()); //client subtracts provided seniors
        foreach (var superJunior in superJuniors)
        {
            p.writeInt(superJunior.getChrId());
            p.writeInt(superJunior.getTotalJuniors());
        }
        p.writeInt(0); //another loop count (entitlements used)
                       //p.writeInt(1); //entitlement index
                       //p.writeInt(2); //times used
        p.writeShort(entry.getJuniorCount() >= 2 ? 0 : 2); //0 disables Add button (only if viewing own pedigree)
        return p;
    }

    private static void addPedigreeEntry(OutPacket p, FamilyEntry entry)
    {
        var chr = entry.getChr();
        bool isOnline = chr != null;
        p.writeInt(entry.getChrId()); //ID
        p.writeInt(entry.getSenior() != null ? entry.getSenior()!.getChrId() : 0); //parent ID
        p.writeShort(entry.getJob().getId()); //job id
        p.writeByte(entry.getLevel()); //level
        p.writeBool(isOnline); //isOnline
        p.writeInt(entry.getReputation()); //current rep
        p.writeInt(entry.getTotalReputation()); //total rep
        p.writeInt(entry.getRepsToSenior()); //reps recorded to senior
        p.writeInt(entry.getTodaysRep());
        p.writeInt(isOnline ? ((chr!.isAwayFromWorld() || chr.getCashShop().isOpened()) ? -1 : chr.getClient().getChannel() - 1) : 0);
        p.writeInt(isOnline ? (int)(chr!.getLoggedInTime().Minutes) : 0); //time online in minutes
        p.writeString(entry.getName()); //name
    }

    public static Packet updateAreaInfo(int area, string info)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        p.writeByte(0x0A); //0x0B in v95
        p.writeShort(area);//infoNumber
        p.writeString(info);
        return p;
    }

    public static Packet getGPMessage(int gpChange)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        p.writeByte(6);
        p.writeInt(gpChange);
        return p;
    }

    public static Packet getItemMessage(int itemid)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        p.writeByte(7);
        p.writeInt(itemid);
        return p;
    }

    public static Packet addCard(bool full, int cardid, int level)
    {
        OutPacket p = OutPacket.create(SendOpcode.MONSTER_BOOK_SET_CARD);
        p.writeByte(full ? 0 : 1);
        p.writeInt(cardid);
        p.writeInt(level);
        return p;
    }

    public static Packet showGainCard()
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_ITEM_GAIN_INCHAT);
        p.writeByte(0x0D);
        return p;
    }

    public static Packet showForeignCardEffect(int id)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_FOREIGN_EFFECT);
        p.writeInt(id);
        p.writeByte(0x0D);
        return p;
    }

    public static Packet changeCover(int cardid)
    {
        OutPacket p = OutPacket.create(SendOpcode.MONSTER_BOOK_SET_COVER);
        p.writeInt(cardid);
        return p;
    }

    public static Packet aranGodlyStats()
    {
        OutPacket p = OutPacket.create(SendOpcode.FORCED_STAT_SET);
        p.writeBytes(new byte[]{
                 0x1F,  0x0F, 0, 0,
                 0xE7, 3,  0xE7, 3,
                 0xE7, 3,  0xE7, 3,
                 0xFF, 0,  0xE7, 3,
                 0xE7, 3,  0x78,  0x8C});
        return p;
    }

    public static Packet showIntro(string path)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_ITEM_GAIN_INCHAT);
        p.writeByte(0x12);
        p.writeString(path);
        return p;
    }

    public static Packet showInfo(string path)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_ITEM_GAIN_INCHAT);
        p.writeByte(0x17);
        p.writeString(path);
        p.writeInt(1);
        return p;
    }

    public static Packet showForeignInfo(int cid, string path)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_FOREIGN_EFFECT);
        p.writeInt(cid);
        p.writeByte(0x17);
        p.writeString(path);
        p.writeInt(1);
        return p;
    }

    /**
     * Sends a UI utility. 0x01 - Equipment Inventory. 0x02 - Stat Window. 0x03
     * - Skill Window. 0x05 - Keyboard Settings. 0x06 - Quest window. 0x09 -
     * Monsterbook Window. 0x0A - Char Info 0x0B - Guild BBS 0x12 - NewMonster
     * Carnival Window 0x16 - Party Search. 0x17 - Item Creation Window. 0x1A -
     * My Ranking O.O 0x1B - Family Window 0x1C - Family Pedigree 0x1D - GM
     * Story Board /funny shet 0x1E - Envelop saying you got mail from an admin.
     * lmfao 0x1F - Medal Window 0x20 - Maple Event (???) 0x21 - Invalid Pointer
     * Crash
     *
     * @param ui
     * @return
     */
    public static Packet openUI(byte ui)
    {
        OutPacket p = OutPacket.create(SendOpcode.OPEN_UI);
        p.writeByte(ui);
        return p;
    }

    public static Packet lockUI(bool enable)
    {
        OutPacket p = OutPacket.create(SendOpcode.LOCK_UI);
        p.writeByte(enable ? 1 : 0);
        return p;
    }

    public static Packet disableUI(bool enable)
    {
        OutPacket p = OutPacket.create(SendOpcode.DISABLE_UI);
        p.writeByte(enable ? 1 : 0);
        return p;
    }

    public static Packet itemMegaphone(string msg, bool whisper, int channel, Item? item)
    {
        OutPacket p = OutPacket.create(SendOpcode.SERVERMESSAGE);
        p.writeByte(8);
        p.writeString(msg);
        p.writeByte(channel - 1);
        p.writeByte(whisper ? 1 : 0);
        if (item == null)
        {
            p.writeByte(0);
        }
        else
        {
            p.writeByte(item.getPosition());
            addItemInfo(p, item, true);
        }
        return p;
    }

    public static Packet removeNPC(int objId)
    {
        OutPacket p = OutPacket.create(SendOpcode.REMOVE_NPC);
        p.writeInt(objId);
        return p;
    }

    public static Packet removeNPCController(int objId)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_NPC_REQUEST_CONTROLLER);
        p.writeByte(0);
        p.writeInt(objId);
        return p;
    }

    /**
     * Sends a report response
     * <p>
     * Possible values for <code>mode</code>:<br> 0: You have succesfully
     * reported the user.<br> 1: Unable to locate the user.<br> 2: You may only
     * report users 10 times a day.<br> 3: You have been reported to the GM's by
     * a user.<br> 4: Your request did not go through for unknown reasons.
     * Please try again later.<br>
     *
     * @param mode The mode
     * @return Report Reponse packet
     */
    public static Packet reportResponse(byte mode)
    {
        OutPacket p = OutPacket.create(SendOpcode.SUE_CHARACTER_RESULT);
        p.writeByte(mode);
        return p;
    }

    public static Packet sendHammerData(int hammerUsed)
    {
        OutPacket p = OutPacket.create(SendOpcode.VICIOUS_HAMMER);
        p.writeByte(0x39);
        p.writeInt(0);
        p.writeInt(hammerUsed);
        return p;
    }

    public static Packet sendHammerMessage()
    {
        OutPacket p = OutPacket.create(SendOpcode.VICIOUS_HAMMER);
        p.writeByte(0x3D);
        p.writeInt(0);
        return p;
    }

    public static Packet playPortalSound()
    {
        return showSpecialEffect(7);
    }

    public static Packet showMonsterBookPickup()
    {
        return showSpecialEffect(14);
    }

    public static Packet showEquipmentLevelUp()
    {
        return showSpecialEffect(15);
    }

    public static Packet showItemLevelup()
    {
        return showSpecialEffect(15);
    }

    /**
     * 0 = Levelup 6 = Exp did not drop (Safety Charms) 7 = Enter portal sound
     * 8 = Job change 9 = Quest complete 10 = Recovery 11 = Buff effect
     * 14 = NewMonster book pickup 15 = Equipment levelup 16 = Maker Skill Success
     * 17 = Buff effect w/ sfx 19 = Exp card [500, 200, 50] 21 = Wheel of destiny
     * 26 = Spirit Stone
     *
     * @param effect
     * @return
     */
    public static Packet showSpecialEffect(int effect)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_ITEM_GAIN_INCHAT);
        p.writeByte(effect);
        return p;
    }

    public static Packet showMakerEffect(bool makerSucceeded)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_ITEM_GAIN_INCHAT);
        p.writeByte(16);
        p.writeInt(makerSucceeded ? 0 : 1);
        return p;
    }

    public static Packet showForeignMakerEffect(int cid, bool makerSucceeded)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_FOREIGN_EFFECT);
        p.writeInt(cid);
        p.writeByte(16);
        p.writeInt(makerSucceeded ? 0 : 1);
        return p;
    }

    public static Packet showForeignEffect(int effect)
    {
        return showForeignEffect(-1, effect);
    }

    public static Packet showForeignEffect(int chrId, int effect)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_FOREIGN_EFFECT);
        p.writeInt(chrId);
        p.writeByte(effect);
        return p;
    }

    public static Packet showOwnRecovery(byte heal)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_ITEM_GAIN_INCHAT);
        p.writeByte(0x0A);
        p.writeByte(heal);
        return p;
    }

    public static Packet showRecovery(int chrId, byte amount)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_FOREIGN_EFFECT);
        p.writeInt(chrId);
        p.writeByte(0x0A);
        p.writeByte(amount);
        return p;
    }

    public static Packet showWheelsLeft(int left)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_ITEM_GAIN_INCHAT);
        p.writeByte(0x15);
        p.writeByte(left);
        return p;
    }

    public static Packet updateQuestFinish(short quest, int npc, short nextquest)
    { //Check
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_QUEST_INFO); //0xF2 in v95
        p.writeByte(8);//0x0A in v95
        p.writeShort(quest);
        p.writeInt(npc);
        p.writeShort(nextquest);
        return p;
    }

    public static Packet showInfoText(string text)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        p.writeByte(9);
        p.writeString(text);
        return p;
    }

    public static Packet questError(short quest)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_QUEST_INFO);
        p.writeByte(0x0A);
        p.writeShort(quest);
        return p;
    }

    public static Packet questFailure(byte type)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_QUEST_INFO);
        p.writeByte(type);//0x0B = No meso, 0x0D = Worn by character, 0x0E = Not having the item ?
        return p;
    }

    public static Packet questExpire(short quest)
    {
        OutPacket p = OutPacket.create(SendOpcode.UPDATE_QUEST_INFO);
        p.writeByte(0x0F);
        p.writeShort(quest);
        return p;
    }

    // MAKER_RESULT packets thanks to Arnah (Vertisy)
    public static Packet makerResult(bool success, int itemMade, int itemCount, int mesos, List<ItemQuantity> itemsLost, int catalystID, List<int> INCBuffGems)
    {
        OutPacket p = OutPacket.create(SendOpcode.MAKER_RESULT);
        p.writeInt(success ? 0 : 1); // 0 = success, 1 = fail
        p.writeInt(1); // 1 or 2 doesn't matter, same methods
        p.writeBool(!success);
        if (success)
        {
            p.writeInt(itemMade);
            p.writeInt(itemCount);
        }
        p.writeInt(itemsLost.Count); // Loop
        foreach (var item in itemsLost)
        {
            p.writeInt(item.ItemId);
            p.writeInt(item.Quantity);
        }
        p.writeInt(INCBuffGems.Count);
        foreach (int gem in INCBuffGems)
        {
            p.writeInt(gem);
        }
        if (catalystID != -1)
        {
            p.writeByte(1); // stimulator
            p.writeInt(catalystID);
        }
        else
        {
            p.writeByte(0);
        }

        p.writeInt(mesos);
        return p;
    }

    public static Packet makerResultCrystal(int itemIdGained, int itemIdLost)
    {
        OutPacket p = OutPacket.create(SendOpcode.MAKER_RESULT);
        p.writeInt(0); // Always successful!
        p.writeInt(3); // NewMonster Crystal
        p.writeInt(itemIdGained);
        p.writeInt(itemIdLost);
        return p;
    }

    public static Packet makerResultDesynth(int itemId, int mesos, List<ItemQuantity> itemsGained)
    {
        OutPacket p = OutPacket.create(SendOpcode.MAKER_RESULT);
        p.writeInt(0); // Always successful!
        p.writeInt(4); // Mode Desynth
        p.writeInt(itemId); // Item desynthed
        p.writeInt(itemsGained.Count); // Loop of items gained, (int, int)
        foreach (var item in itemsGained)
        {
            p.writeInt(item.ItemId);
            p.writeInt(item.Quantity);
        }
        p.writeInt(mesos); // Mesos spent.
        return p;
    }

    public static Packet makerEnableActions()
    {
        OutPacket p = OutPacket.create(SendOpcode.MAKER_RESULT);
        p.writeInt(0); // Always successful!
        p.writeInt(0); // NewMonster Crystal
        p.writeInt(0);
        p.writeInt(0);
        return p;
    }

    public static Packet getMultiMegaphone(string[] messages, int channel, bool showEar)
    {
        OutPacket p = OutPacket.create(SendOpcode.SERVERMESSAGE);
        p.writeByte(0x0A);
        if (messages[0] != null)
        {
            p.writeString(messages[0]);
        }
        p.writeByte(messages.Length);
        for (int i = 1; i < messages.Length; i++)
        {
            if (messages[i] != null)
            {
                p.writeString(messages[i]);
            }
        }
        for (int i = 0; i < 10; i++)
        {
            p.writeByte(channel - 1);
        }
        p.writeByte(showEar ? 1 : 0);
        p.writeByte(1);
        return p;
    }

    /**
     * Gets a gm effect packet (ie. hide, banned, etc.)
     * <p>
     * Possible values for <code>type</code>:<br> 0x04: You have successfully
     * blocked access.<br>
     * 0x05: The unblocking has been successful.<br> 0x06 with Mode 0: You have
     * successfully removed the name from the ranks.<br> 0x06 with Mode 1: You
     * have entered an invalid character name.<br> 0x10: GM Hide, mode
     * determines whether or not it is on.<br> 0x1E: Mode 0: Failed to send
     * warning Mode 1: Sent warning<br> 0x13 with Mode 0: + mapid 0x13 with Mode
     * 1: + ch (FF = Unable to find merchant)
     *
     * @param type The type
     * @param mode The mode
     * @return The gm effect packet
     */
    public static Packet getGMEffect(int type, byte mode)
    {
        OutPacket p = OutPacket.create(SendOpcode.ADMIN_RESULT);
        p.writeByte(type);
        p.writeByte(mode);
        return p;
    }

    public static Packet findMerchantResponse(bool map, int extra)
    {
        OutPacket p = OutPacket.create(SendOpcode.ADMIN_RESULT);
        p.writeByte(0x13);
        p.writeByte(map ? 0 : 1); //00 = mapid, 01 = ch
        if (map)
        {
            p.writeInt(extra);
        }
        else
        {
            p.writeByte(extra); //-1 = unable to find
        }
        p.writeByte(0);
        return p;
    }

    public static Packet disableMinimap()
    {
        OutPacket p = OutPacket.create(SendOpcode.ADMIN_RESULT);
        p.writeShort(0x1C);
        return p;
    }

    public static Packet sendFamilyInvite(int playerId, string inviter)
    {
        OutPacket p = OutPacket.create(SendOpcode.FAMILY_JOIN_REQUEST);
        p.writeInt(playerId);
        p.writeString(inviter);
        return p;
    }

    public static Packet sendFamilySummonRequest(string familyName, string from)
    {
        OutPacket p = OutPacket.create(SendOpcode.FAMILY_SUMMON_REQUEST);
        p.writeString(from);
        p.writeString(familyName);
        return p;
    }

    public static Packet sendFamilyLoginNotice(string name, bool loggedIn)
    {
        OutPacket p = OutPacket.create(SendOpcode.FAMILY_NOTIFY_LOGIN_OR_LOGOUT);
        p.writeBool(loggedIn);
        p.writeString(name);
        return p;
    }

    public static Packet sendFamilyJoinResponse(bool accepted, string added)
    {
        OutPacket p = OutPacket.create(SendOpcode.FAMILY_JOIN_REQUEST_RESULT);
        p.writeByte(accepted ? 1 : 0);
        p.writeString(added);
        return p;
    }

    public static Packet getSeniorMessage(string name)
    {
        OutPacket p = OutPacket.create(SendOpcode.FAMILY_JOIN_ACCEPTED);
        p.writeString(name);
        p.writeInt(0);
        return p;
    }

    public static Packet sendGainRep(int gain, string from)
    {
        OutPacket p = OutPacket.create(SendOpcode.FAMILY_REP_GAIN);
        p.writeInt(gain);
        p.writeString(from);
        return p;
    }

    public static Packet showBoughtCashPackage(List<Item> cashPackage, int accountId)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);

        p.writeByte(0x89);
        p.writeByte(cashPackage.Count);

        foreach (Item item in cashPackage)
        {
            addCashItemInformation(p, item, accountId);
        }

        p.writeShort(0);

        return p;
    }

    public static Packet showBoughtQuestItem(int itemId)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);
        p.writeByte(0x8D);
        p.writeInt(1);
        p.writeShort(1);
        p.writeByte(0x0B);
        p.writeByte(0);
        p.writeInt(itemId);
        return p;
    }

    // Cash Shop Surprise packets found thanks to Arnah (Vertisy)
    public static Packet onCashItemGachaponOpenFailed()
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_CASH_ITEM_GACHAPON_RESULT);
        p.writeByte(0xE4);
        return p;
    }

    public static Packet onCashGachaponOpenSuccess(int accountid, long boxCashId, int remainingBoxes, Item reward,
                                                   int rewardItemId, int rewardQuantity, bool bJackpot)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_CASH_ITEM_GACHAPON_RESULT);
        p.writeByte(0xE5);   // subopcode thanks to Ubaware
        p.writeLong(boxCashId);
        p.writeInt(remainingBoxes);
        addCashItemInformation(p, reward, accountid);
        p.writeInt(rewardItemId);
        p.writeByte(rewardQuantity); // nSelectedItemCount
        p.writeBool(bJackpot);// "CashGachaponJackpot"
        return p;
    }

    public static Packet sendMesoLimit()
    {
        OutPacket p = OutPacket.create(SendOpcode.TRADE_MONEY_LIMIT); //Players under level 15 can only trade 1m per day
        return p;
    }

    public static Packet removeItemFromDuey(bool remove, int Package)
    {
        OutPacket p = OutPacket.create(SendOpcode.PARCEL);
        p.writeByte(0x17);
        p.writeInt(Package);
        p.writeByte(remove ? 3 : 4);
        return p;
    }

    public static Packet sendDueyParcelReceived(string from, bool quick)
    {    // thanks inhyuk
        OutPacket p = OutPacket.create(SendOpcode.PARCEL);
        p.writeByte(0x19);
        p.writeString(from);
        p.writeBool(quick);
        return p;
    }

    public static Packet sendDueyParcelNotification(bool quick)
    {
        OutPacket p = OutPacket.create(SendOpcode.PARCEL);
        p.writeByte(0x1B);
        p.writeBool(quick);  // 0 : package received, 1 : quick delivery namespace return p;
        return p;
    }

    public static Packet sendDueyMSG(byte operation)
    {
        return sendDuey(operation, []);
    }

    public static Packet sendDuey(int operation, List<Dueypackage> packages)
    {
        OutPacket p = OutPacket.create(SendOpcode.PARCEL);
        p.writeByte(operation);
        if (operation == 8)
        {
            p.writeByte(0);
            p.writeByte(packages.Count);
            foreach (var dp in packages)
            {
                p.writeInt(dp.PackageId);
                p.writeFixedString(dp.SenderName);

                p.writeInt(dp.Mesos);
                p.writeLong(getTime(dp.sentTimeInMilliseconds()));

                var msg = dp.Message;
                if (msg != null)
                {
                    p.writeInt(1);
                    p.writeFixedString(msg, 200);
                }
                else
                {
                    p.writeInt(0);
                    p.skip(200);
                }

                p.writeByte(0);
                if (dp.Item != null)
                {
                    p.writeByte(1);
                    addItemInfo(p, dp.Item, true);
                }
                else
                {
                    p.writeByte(0);
                }
            }
            p.writeByte(0);
        }

        return p;
    }

    public static Packet sendDojoAnimation(byte firstByte, string animation)
    {
        OutPacket p = OutPacket.create(SendOpcode.FIELD_EFFECT);
        p.writeByte(firstByte);
        p.writeString(animation);
        return p;
    }

    public static Packet getDojoInfo(string info)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        p.writeByte(10);
        p.writeBytes(new byte[] { 0xB7, 4 });//QUEST ID f5
        p.writeString(info);
        return p;
    }

    public static Packet getDojoInfoMessage(string message)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        p.writeByte(9);
        p.writeString(message);
        return p;
    }

    /**
     * Gets a "block" packet (ie. the cash shop is unavailable, etc)
     * <p>
     * Possible values for <code>type</code>:<br> 1: The portal is closed for
     * now.<br> 2: You cannot go to that place.<br> 3: Unable to approach due to
     * the force of the ground.<br> 4: You cannot teleport to or on this
     * map.<br> 5: Unable to approach due to the force of the ground.<br> 6:
     * Only party members can enter this map.<br> 7: The Cash Shop is
     * currently not available. Stay tuned...<br>
     *
     * @param type The type
     * @return The "block" packet.
     */
    public static Packet blockedMessage(int type)
    {
        OutPacket p = OutPacket.create(SendOpcode.BLOCKED_MAP);
        p.writeByte(type);
        return p;
    }

    /**
     * Gets a "block" packet (ie. the cash shop is unavailable, etc)
     * <p>
     * Possible values for <code>type</code>:<br> 1: You cannot move that
     * channel. Please try again later.<br> 2: You cannot go into the cash shop.
     * Please try again later.<br> 3: The Item-Trading Shop is currently
     * unavailable. Please try again later.<br> 4: You cannot go into the trade
     * shop, due to limitation of user count.<br> 5: You do not meet the minimum
     * level requirement to access the Trade Shop.<br>
     *
     * @param type The type
     * @return The "block" packet.
     */
    public static Packet blockedMessage2(int type)
    {
        OutPacket p = OutPacket.create(SendOpcode.BLOCKED_SERVER);
        p.writeByte(type);
        return p;
    }

    public static Packet updateDojoStats(IPlayer chr, int belt)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        p.writeByte(10);
        p.writeBytes(new byte[] { 0xB7, 4 }); //?
        p.writeString("pt=" + chr.getDojoPoints() + ";belt=" + belt + ";tuto=" + (chr.getFinishedDojoTutorial() ? "1" : "0"));
        return p;
    }

    /**
     * Sends a "levelup" packet to the guild or family.
     * <p>
     * Possible values for <code>type</code>:<br> 0: <Family> ? has reached Lv.
     * ?.<br> - The Reps you have received from ? will be reduced in half. 1:
     * <Family> ? has reached Lv. ?.<br> 2: <Guild> ? has reached Lv. ?.<br>
     *
     * @param type The type
     * @return The "levelup" packet.
     */
    public static Packet levelUpMessage(int type, int level, string charname)
    {
        OutPacket p = OutPacket.create(SendOpcode.NOTIFY_LEVELUP);
        p.writeByte(type);
        p.writeInt(level);
        p.writeString(charname);

        return p;
    }

    /**
     * Sends a "married" packet to the guild or family.
     * <p>
     * Possible values for <code>type</code>:<br> 0: <Guild ? is now married.
     * Please congratulate them.<br> 1: <Family ? is now married. Please
     * congratulate them.<br>
     *
     * @param type The type
     * @return The "married" packet.
     */
    public static Packet marriageMessage(int type, string charname)
    {
        OutPacket p = OutPacket.create(SendOpcode.NOTIFY_MARRIAGE);
        p.writeByte(type);  // 0: guild, 1: family
        p.writeString("> " + charname); //To fix the stupid packet lol

        return p;
    }

    /**
     * Sends a "job advance" packet to the guild or family.
     * <p>
     * Possible values for <code>type</code>:<br> 0: <Guild ? has advanced to
     * a(an) ?.<br> 1: <Family ? has advanced to a(an) ?.<br>
     *
     * @param type The type
     * @return The "job advance" packet.
     */
    public static Packet jobMessage(int type, int job, string charname)
    {
        OutPacket p = OutPacket.create(SendOpcode.NOTIFY_JOB_CHANGE);
        p.writeByte(type);
        p.writeInt(job); //Why fking int?
        p.writeString("> " + charname); //To fix the stupid packet lol
        return p;
    }

    /**
     * @param type  - (0:Light&long 1:Heavy&short)
     * @param delay - seconds
     * @return
     */
    public static Packet trembleEffect(int type, int delay)
    {
        OutPacket p = OutPacket.create(SendOpcode.FIELD_EFFECT);
        p.writeByte(1);
        p.writeByte(type);
        p.writeInt(delay);
        return p;
    }

    public static Packet getEnergy(string info, int amount)
    {
        OutPacket p = OutPacket.create(SendOpcode.SESSION_VALUE);
        p.writeString(info);
        p.writeString(amount.ToString());
        return p;
    }

    public static Packet dojoWarpUp()
    {
        OutPacket p = OutPacket.create(SendOpcode.DOJO_WARP_UP);
        p.writeByte(0);
        p.writeByte(6);
        return p;
    }

    public static Packet itemExpired(int itemid)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        p.writeByte(2);
        p.writeInt(itemid);
        return p;
    }

    private static string getRightPaddedStr(string inValue, char padchar, int length)
    {
        return inValue.PadRight(length, padchar);
    }

    public static Packet MobDamageMobFriendly(Monster mob, int damage, int remainingHp)
    {
        OutPacket p = OutPacket.create(SendOpcode.DAMAGE_MONSTER);
        p.writeInt(mob.getObjectId());
        p.writeByte(1); // direction ?
        p.writeInt(damage);
        p.writeInt(remainingHp);
        p.writeInt(mob.getMaxHp());
        return p;
    }

    public static Packet shopErrorMessage(int error, int type)
    {
        OutPacket p = OutPacket.create(SendOpcode.PLAYER_INTERACTION);
        p.writeByte(0x0A);
        p.writeByte(type);
        p.writeByte(error);
        return p;
    }

    private static void addRingInfo(OutPacket p, IPlayer chr)
    {
        p.writeShort(chr.getCrushRings().Count);
        foreach (Ring ring in chr.getCrushRings())
        {
            p.writeInt(ring.getPartnerChrId());
            p.writeFixedString(ring.getPartnerName());
            p.writeInt(ring.getRingId());
            p.writeInt(0);
            p.writeInt(ring.getPartnerRingId());
            p.writeInt(0);
        }
        p.writeShort(chr.getFriendshipRings().Count);
        foreach (Ring ring in chr.getFriendshipRings())
        {
            p.writeInt(ring.getPartnerChrId());
            p.writeFixedString(ring.getPartnerName());
            p.writeInt(ring.getRingId());
            p.writeInt(0);
            p.writeInt(ring.getPartnerRingId());
            p.writeInt(0);
            p.writeInt(ring.getItemId());
        }

        if (chr.getPartnerId() > 0)
        {
            var marriageRing = chr.getMarriageRing();

            p.writeShort(1);
            p.writeInt(chr.getRelationshipId());
            p.writeInt(chr.getGender() == 0 ? chr.getId() : chr.getPartnerId());
            p.writeInt(chr.getGender() == 0 ? chr.getPartnerId() : chr.getId());
            p.writeShort((marriageRing != null) ? 3 : 1);
            if (marriageRing != null)
            {
                p.writeInt(marriageRing.getItemId());
                p.writeInt(marriageRing.getItemId());
            }
            else
            {
                p.writeInt(ItemId.WEDDING_RING_MOONSTONE); // Engagement Ring's Outcome (doesn't matter for engagement)
                p.writeInt(ItemId.WEDDING_RING_MOONSTONE); // Engagement Ring's Outcome (doesn't matter for engagement)
            }
            p.writeFixedString(chr.getGender() == 0 ? chr.getName() : CharacterManager.getNameById(chr.getPartnerId()));
            p.writeFixedString(chr.getGender() == 0 ? CharacterManager.getNameById(chr.getPartnerId()) : chr.getName());
        }
        else
        {
            p.writeShort(0);
        }
    }

    public static Packet finishedSort(int inv)
    {
        OutPacket p = OutPacket.create(SendOpcode.GATHER_ITEM_RESULT);
        p.writeByte(0);
        p.writeByte(inv);
        return p;
    }

    public static Packet finishedSort2(int inv)
    {
        OutPacket p = OutPacket.create(SendOpcode.SORT_ITEM_RESULT);
        p.writeByte(0);
        p.writeByte(inv);
        return p;
    }

    public static Packet bunnyPacket()
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_STATUS_INFO);
        p.writeByte(9);
        p.writeFixedString("Protect the Moon Bunny!!!");
        return p;
    }

    public static Packet hpqMessage(string text)
    {
        OutPacket p = OutPacket.create(SendOpcode.BLOW_WEATHER); // not 100% sure
        p.writeByte(0);
        p.writeInt(ItemId.NPC_WEATHER_GROWLIE);
        p.writeFixedString(text);
        return p;
    }

    public static Packet showEventInstructions()
    {
        OutPacket p = OutPacket.create(SendOpcode.GMEVENT_INSTRUCTIONS);
        p.writeByte(0);
        return p;
    }

    public static Packet leftKnockBack()
    {
        return OutPacket.create(SendOpcode.LEFT_KNOCK_BACK);
    }

    public static Packet rollSnowBall(bool entermap, int state, Snowball ball0, Snowball ball1)
    {
        OutPacket p = OutPacket.create(SendOpcode.SNOWBALL_STATE);
        if (entermap)
        {
            p.skip(21);
        }
        else
        {
            p.writeByte(state);// 0 = move, 1 = roll, 2 is down disappear, 3 is up disappear
            p.writeInt(ball0.getSnowmanHP() / 75);
            p.writeInt(ball1.getSnowmanHP() / 75);
            p.writeShort(ball0.getPosition());//distance snowball down, 84 03 = max
            p.writeByte(-1);
            p.writeShort(ball1.getPosition());//distance snowball up, 84 03 = max
            p.writeByte(-1);
        }
        return p;
    }

    public static Packet rollSnowBall()
    {
        OutPacket p = OutPacket.create(SendOpcode.SNOWBALL_STATE);
        p.skip(21);
        return p;
    }

    public static Packet hitSnowBall(int what, int damage)
    {
        OutPacket p = OutPacket.create(SendOpcode.HIT_SNOWBALL);
        p.writeByte(what);
        p.writeInt(damage);
        return p;
    }

    /**
     * Sends a Snowball Message<br>
     * <p>
     * Possible values for <code>message</code>:<br> 1: ... Team's snowball has
     * passed the stage 1.<br> 2: ... Team's snowball has passed the stage
     * 2.<br> 3: ... Team's snowball has passed the stage 3.<br> 4: ... Team is
     * attacking the snowman, stopping the progress<br> 5: ... Team is moving
     * again<br>
     *
     * @param message
     */
    public static Packet snowballMessage(int team, int message)
    {
        OutPacket p = OutPacket.create(SendOpcode.SNOWBALL_MESSAGE);
        p.writeByte(team);// 0 is down, 1 is up
        p.writeInt(message);
        return p;
    }

    public static Packet coconutScore(int team1, int team2)
    {
        OutPacket p = OutPacket.create(SendOpcode.COCONUT_SCORE);
        p.writeShort(team1);
        p.writeShort(team2);
        return p;
    }

    public static Packet hitCoconut(bool spawn, int id, int type)
    {
        OutPacket p = OutPacket.create(SendOpcode.COCONUT_HIT);
        if (spawn)
        {
            p.writeShort(-1);
            p.writeShort(5000);
            p.writeByte(0);
        }
        else
        {
            p.writeShort(id);
            p.writeShort(1000);//delay till you can attack again!
            p.writeByte(type); // What action to do for the coconut.
        }
        return p;
    }

    public static Packet customPacket(string packet)
    {
        OutPacket p = new ByteBufOutPacket();
        p.writeBytes(HexTool.toBytes(packet));
        return p;
    }

    public static Packet customPacket(byte[] packet)
    {
        OutPacket p = new ByteBufOutPacket();
        p.writeBytes(packet);
        return p;
    }

    public static Packet spawnGuide(bool spawn)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_GUIDE);
        p.writeBool(spawn);
        return p;
    }

    public static Packet talkGuide(string talk)
    {
        OutPacket p = OutPacket.create(SendOpcode.TALK_GUIDE);
        p.writeByte(0);
        p.writeString(talk);
        p.writeBytes(new byte[] { 0xC8, 0, 0, 0, 0xA0, 0x0F, 0, 0 });
        return p;
    }

    public static Packet guideHint(int hint)
    {
        OutPacket p = OutPacket.create(SendOpcode.TALK_GUIDE);
        p.writeByte(1);
        p.writeInt(hint);
        p.writeInt(7000);
        return p;
    }

    public static void addCashItemInformation(OutPacket p, Item item, int accountId)
    {
        addCashItemInformation(p, item, accountId, null);
    }

    public static void addCashItemInformation(OutPacket p, Item item, int accountId, string? giftMessage)
    {
        bool isGift = giftMessage != null;
        bool isRing = false;
        Equip? equip = null;
        if (item.getInventoryType().Equals(InventoryType.EQUIP))
        {
            equip = (Equip)item;
            isRing = equip.getRingId() > -1;
        }
        p.writeLong(item.getPetId() > -1 ? item.getPetId() : isRing ? equip!.getRingId() : item.getCashId());
        if (!isGift)
        {
            p.writeInt(accountId);
            p.writeInt(0);
        }
        p.writeInt(item.getItemId());
        if (!isGift)
        {
            p.writeInt(item.getSN());
            p.writeShort(item.getQuantity());
        }
        p.writeFixedString(item.getGiftFrom());
        if (isGift)
        {
            p.writeFixedString(giftMessage, 73);
            return;
        }
        addExpirationTime(p, item.getExpiration());
        p.writeLong(0);
    }

    public static Packet showWishList(IPlayer mc, bool update)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);

        if (update)
        {
            p.writeByte(0x55);
        }
        else
        {
            p.writeByte(0x4F);
        }

        foreach (int sn in mc.getCashShop().getWishList())
        {
            p.writeInt(sn);
        }

        for (int i = mc.getCashShop().getWishList().Count; i < 10; i++)
        {
            p.writeInt(0);
        }

        return p;
    }

    public static Packet showBoughtCashItem(Item item, int accountId)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);

        p.writeByte(0x57);
        addCashItemInformation(p, item, accountId);

        return p;
    }

    public static Packet showBoughtCashRing(Item ring, string recipient, int accountId)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);
        p.writeByte(0x87);
        addCashItemInformation(p, ring, accountId);
        p.writeString(recipient);
        p.writeInt(ring.getItemId());
        p.writeShort(1); //quantity
        return p;
    }

    /*
     * 00 = Due to an unknown error, failed
     * A3 = Request timed out. Please try again.
     * A4 = Due to an unknown error, failed + warpout
     * A5 = You don't have enough cash.
     * A6 = long as shet msg
     * A7 = You have exceeded the allotted limit of price for gifts.
     * A8 = You cannot send a gift to your own account. Log in on the char and purchase
     * A9 = Please confirm whether the character's name is correct.
     * AA = Gender restriction!
     * AB = gift cannot be sent because recipient inv is full
     * AC = exceeded the number of cash items you can have
     * AD = check and see if the character name is wrong or there is gender restrictions
     * //Skipped a few
     * B0 = Wrong Coupon Code
     * B1 = Disconnect from CS because of 3 wrong coupon codes < lol
     * B2 = Expired Coupon
     * B3 = Coupon has been used already
     * B4 = Nexon internet cafes? lolfk
     * B8 = Due to gender restrictions, the coupon cannot be used.
     * BB = inv full
     * BC = long as shet "(not?) available to purchase by a use at the premium" msg
     * BD = invalid gift recipient
     * BE = invalid receiver name
     * BF = item unavailable to purchase at this hour
     * C0 = not enough items in stock, therefore not available
     * C1 = you have exceeded spending limit of NX
     * C2 = not enough mesos? Lol not even 1 mesos xD
     * C3 = cash shop unavailable during beta phase
     * C4 = check birthday code
     * C7 = only available to users buying cash item, whatever msg too long
     * C8 = already applied for this
     * CD = You have reached the daily purchase limit for the cash shop.
     * D0 = coupon account limit reached
     * D2 = coupon system currently unavailable
     * D3 = item can only be used 15 days after registration
     * D4 = not enough gift tokens
     * D6 = fresh people cannot gift items lul
     * D7 = bad people cannot gift items >:(
     * D8 = cannot gift due to limitations
     * D9 = cannot gift due to amount of gifted times
     * DA = cannot be gifted due to technical difficulties
     * DB = cannot transfer to char below level 20
     * DC = cannot transfer char to same world
     * DD = cannot transfer char to new server world
     * DE = cannot transfer char out of this world
     * DF = cannot transfer char due to no empty char slots
     * E0 = event or free test time ended
     * E6 = item cannot be purchased with MaplePoints
     * E7 = lol sorry for the inconvenience, eh?
     * E8 = cannot purchase by anyone under 7
     */
    public static Packet showCashShopMessage(byte message)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);
        p.writeByte(0x5C);
        p.writeByte(message);
        return p;
    }

    public static Packet showCashInventory(IClient c)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);

        p.writeByte(0x4B);
        p.writeShort(c.OnlinedCharacter.getCashShop().getInventory().Count);

        foreach (Item item in c.OnlinedCharacter.getCashShop().getInventory())
        {
            addCashItemInformation(p, item, c.getAccID());
        }

        p.writeShort(c.OnlinedCharacter.getStorage().getSlots());
        p.writeShort(c.getCharacterSlots());

        return p;
    }

    public static Packet showGifts(List<ItemMessagePair> gifts)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);

        p.writeByte(0x4D);
        p.writeShort(gifts.Count);

        foreach (var gift in gifts)
        {
            addCashItemInformation(p, gift.Item, 0, gift.Message);
        }

        return p;
    }

    public static Packet showGiftSucceed(string to, CashItem item)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);

        p.writeByte(0x5E); //0x5D, Couldn't be sent
        p.writeString(to);
        p.writeInt(item.getItemId());
        p.writeShort(item.getCount());
        p.writeInt(item.getPrice());

        return p;
    }

    public static Packet showBoughtInventorySlots(int type, short slots)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);

        p.writeByte(0x60);
        p.writeByte(type);
        p.writeShort(slots);

        return p;
    }

    public static Packet showBoughtStorageSlots(short slots)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);

        p.writeByte(0x62);
        p.writeShort(slots);

        return p;
    }

    public static Packet showBoughtCharacterSlot(short slots)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);

        p.writeByte(0x64);
        p.writeShort(slots);

        return p;
    }

    public static Packet takeFromCashInventory(Item item)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);

        p.writeByte(0x68);
        p.writeShort(item.getPosition());
        addItemInfo(p, item, true);

        return p;
    }

    public static Packet deleteCashItem(Item item)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);
        p.writeByte(0x6C);
        p.writeLong(item.getCashId());
        return p;
    }

    public static Packet refundCashItem(Item item, int maplePoints)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);
        p.writeByte(0x85);
        p.writeLong(item.getCashId());
        p.writeInt(maplePoints);
        return p;
    }

    public static Packet putIntoCashInventory(Item item, int accountId)
    {
        OutPacket p = OutPacket.create(SendOpcode.CASHSHOP_OPERATION);

        p.writeByte(0x6A);
        addCashItemInformation(p, item, accountId);

        return p;
    }

    public static Packet openCashShop(IClient c, bool mts)
    {
        OutPacket p = OutPacket.create(mts ? SendOpcode.SET_ITC : SendOpcode.SET_CASH_SHOP);

        addCharacterInfo(p, c.OnlinedCharacter);

        if (!mts)
        {
            p.writeByte(1);
        }

        p.writeString(c.getAccountName());
        if (mts)
        {
            p.writeBytes(new byte[]{ 0x88, 19, 0, 0,
                    7, 0, 0, 0,
                     0xF4, 1, 0, 0,
                     0x18, 0, 0, 0,
                     0xA8, 0, 0, 0,
                     0x70,  0xAA,  0xA7,  0xC5,
                     0x4E,  0xC1,  0xCA, 1});
        }
        else
        {
            p.writeInt(0);
            List<SpecialCashItem> lsci = CashItemFactory.getSpecialCashItems();
            p.writeShort(lsci.Count);//Guess what
            foreach (SpecialCashItem sci in lsci)
            {
                p.writeInt(sci.getSN());
                p.writeInt(sci.getModifier());
                p.writeByte(sci.getInfo());
            }
            p.skip(121);

            List<List<int>> mostSellers = c.getWorldServer().getMostSellerCashItems();
            for (int i = 1; i <= 8; i++)
            {
                List<int> mostSellersTab = mostSellers.get(i);

                for (int j = 0; j < 2; j++)
                {
                    foreach (int snid in mostSellersTab)
                    {
                        p.writeInt(i);
                        p.writeInt(j);
                        p.writeInt(snid);
                    }
                }
            }

            p.writeInt(0);
            p.writeShort(0);
            p.writeByte(0);
            p.writeInt(75);
        }
        return p;
    }

    public static Packet sendVegaScroll(int op)
    {
        OutPacket p = OutPacket.create(SendOpcode.VEGA_SCROLL);
        p.writeByte(op);
        return p;
    }

    public static Packet resetForcedStats()
    {
        return OutPacket.create(SendOpcode.FORCED_STAT_RESET);
    }

    public static Packet showCombo(int count)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHOW_COMBO);
        p.writeInt(count);
        return p;
    }

    public static Packet earnTitleMessage(string msg)
    {
        OutPacket p = OutPacket.create(SendOpcode.SCRIPT_PROGRESS_MESSAGE);
        p.writeString(msg);
        return p;
    }

    public static Packet CPUpdate(bool party, int curCP, int totalCP, int team)
    { // CPQ
        OutPacket p;
        if (!party)
        {
            p = OutPacket.create(SendOpcode.MONSTER_CARNIVAL_OBTAINED_CP);
        }
        else
        {
            p = OutPacket.create(SendOpcode.MONSTER_CARNIVAL_PARTY_CP);
            p.writeByte(team); // team?
        }
        p.writeShort(curCP);
        p.writeShort(totalCP);
        return p;
    }

    public static Packet CPQMessage(byte message)
    {
        OutPacket p = OutPacket.create(SendOpcode.MONSTER_CARNIVAL_MESSAGE);
        p.writeByte(message); // Message
        return p;
    }

    public static Packet playerSummoned(string name, int tab, int number)
    {
        OutPacket p = OutPacket.create(SendOpcode.MONSTER_CARNIVAL_SUMMON);
        p.writeByte(tab);
        p.writeByte(number);
        p.writeString(name);
        return p;
    }

    public static Packet playerDiedMessage(string name, int lostCP, int team)
    { // CPQ
        OutPacket p = OutPacket.create(SendOpcode.MONSTER_CARNIVAL_DIED);
        p.writeByte(team); // team
        p.writeString(name);
        p.writeByte(lostCP);
        return p;
    }

    public static Packet startMonsterCarnival(IPlayer chr, int team, int opposition)
    {
        OutPacket p = OutPacket.create(SendOpcode.MONSTER_CARNIVAL_START);
        p.writeByte(team); // team
        p.writeShort(chr.getCP()); // Obtained CP - Used CP
        p.writeShort(chr.getTotalCP()); // Total Obtained CP
        p.writeShort(chr.getMonsterCarnival()!.getCP(team)); // Obtained CP - Used CP of the team
        p.writeShort(chr.getMonsterCarnival()!.getTotalCP(team)); // Total Obtained CP of the team
        p.writeShort(chr.getMonsterCarnival()!.getCP(opposition)); // Obtained CP - Used CP of the team
        p.writeShort(chr.getMonsterCarnival()!.getTotalCP(opposition)); // Total Obtained CP of the team
        p.writeShort(0); // Probably useless nexon shit
        p.writeLong(0); // Probably useless nexon shit
        return p;
    }

    public static Packet sheepRanchInfo(byte wolf, byte sheep)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHEEP_RANCH_INFO);
        p.writeByte(wolf);
        p.writeByte(sheep);
        return p;
    }
    //Know what this is? ?? >=)

    public static Packet sheepRanchClothes(int id, byte clothes)
    {
        OutPacket p = OutPacket.create(SendOpcode.SHEEP_RANCH_CLOTHES);
        p.writeInt(id); //IPlayer id
        p.writeByte(clothes); //0 = sheep, 1 = wolf, 2 = Spectator (wolf without wool)
        return p;
    }

    public static Packet incubatorResult()
    {//lol
        OutPacket p = OutPacket.create(SendOpcode.INCUBATOR_RESULT);
        p.skip(6);
        return p;
    }

    public static Packet pyramidGauge(int gauge)
    {
        OutPacket p = OutPacket.create(SendOpcode.PYRAMID_GAUGE);
        p.writeInt(gauge);
        return p;
    }
    // f2

    public static Packet pyramidScore(byte score, int exp)
    {//Type cannot be higher than 4 (Rank D), otherwise you'll crash
        OutPacket p = OutPacket.create(SendOpcode.PYRAMID_SCORE);
        p.writeByte(score);
        p.writeInt(exp);
        return p;
    }

    public static Packet spawnDragon(Dragon dragon)
    {
        OutPacket p = OutPacket.create(SendOpcode.SPAWN_DRAGON);
        p.writeInt(dragon.getOwner().getId());//objectid = owner id
        p.writeShort(dragon.getPosition().X);
        p.writeShort(0);
        p.writeShort(dragon.getPosition().Y);
        p.writeShort(0);
        p.writeByte(dragon.getStance());
        p.writeByte(0);
        p.writeShort(dragon.getOwner().getJob().getId());
        return p;
    }

    public static Packet moveDragon(Dragon dragon, Point startPos, InPacket movementPacket, long movementDataLength)
    {
        OutPacket p = OutPacket.create(SendOpcode.MOVE_DRAGON);
        p.writeInt(dragon.getOwner().getId());
        p.writePos(startPos);
        rebroadcastMovementList(p, movementPacket, movementDataLength);
        return p;
    }

    /**
     * Sends a request to remove Mir<br>
     *
     * @param charid - Needs the specific IPlayer ID
     * @return The packet
     */
    public static Packet removeDragon(int chrId)
    {
        OutPacket p = OutPacket.create(SendOpcode.REMOVE_DRAGON);
        p.writeInt(chrId);
        return p;
    }

    /**
     * Changes the current background effect to either being rendered or not.
     * Data is still missing, so this is pretty binary at the moment in how it
     * behaves.
     *
     * @param remove     whether or not the remove or add the specified layer.
     * @param layer      the targeted layer for removal or addition.
     * @param transition the time it takes to transition the effect.
     * @return a packet to change the background effect of a specified layer.
     */
    public static Packet changeBackgroundEffect(bool remove, int layer, int transition)
    {
        OutPacket p = OutPacket.create(SendOpcode.SET_BACK_EFFECT);
        p.writeBool(remove);
        p.writeInt(0); // not sure what this int32 does yet
        p.writeByte(layer);
        p.writeInt(transition);
        return p;
    }

    /**
     * Makes the NPCs provided set as scriptable, informing the client to search for js scripts for these NPCs even
     * if they already have entries within the wz files.
     *
     * @param scriptableNpcIds Ids of npcs to enable scripts for.
     * @return a packet which makes the npc's provided scriptable.
     */
    public static Packet setNPCScriptable(Dictionary<int, string> scriptableNpcIds)
    {  // thanks to GabrielSin
        OutPacket p = OutPacket.create(SendOpcode.SET_NPC_SCRIPTABLE);
        p.writeByte(scriptableNpcIds.Count);
        foreach (var item in scriptableNpcIds)
        {
            var (id, name) = (item.Key, item.Value);
            p.writeInt(id);
            // The client needs a name for the npc conversation, which is displayed under etc when the npc has a quest available.
            p.writeString(name);
            p.writeInt(0); // start time
            p.writeInt(int.MaxValue); // end time
        }
        return p;
    }

    private static Packet MassacreResult(byte nRank, int nIncExp)
    {
        //CField_MassacreResult__OnMassacreResult @ 0x005617C5
        OutPacket p = OutPacket.create(SendOpcode.PYRAMID_SCORE); //MASSACRERESULT | 0x009E
        p.writeByte(nRank); //(0 - S) (1 - A) (2 - B) (3 - C) (4 - D) ( Else - Crash )
        p.writeInt(nIncExp);
        return p;
    }


    private static Packet Tournament__Tournament(byte nState, byte nSubState)
    {
        OutPacket p = OutPacket.create(SendOpcode.TOURNAMENT);
        p.writeByte(nState);
        p.writeByte(nSubState);
        return p;
    }

    private static Packet Tournament__MatchTable(byte nState, byte nSubState)
    {
        OutPacket p = OutPacket.create(SendOpcode.TOURNAMENT_MATCH_TABLE); //Prompts CMatchTableDlg Modal
        return p;
    }

    private static Packet Tournament__SetPrize(byte bSetPrize, byte bHasPrize, int nItemID1, int nItemID2)
    {
        OutPacket p = OutPacket.create(SendOpcode.TOURNAMENT_SET_PRIZE);

        //0 = "You have failed the set the prize. Please check the item number again."
        //1 = "You have successfully set the prize."
        p.writeByte(bSetPrize);

        p.writeByte(bHasPrize);

        if (bHasPrize != 0)
        {
            p.writeInt(nItemID1);
            p.writeInt(nItemID2);
        }

        return p;
    }

    private static Packet Tournament__UEW(byte nState)
    {
        OutPacket p = OutPacket.create(SendOpcode.TOURNAMENT_UEW);

        //Is this a bitflag o.o ?
        //2 = "You have reached the finals by default."
        //4 = "You have reached the semifinals by default."
        //8 or 16 = "You have reached the round of %n by default." | Encodes nState as %n ?!
        p.writeByte(nState);

        return p;
    }

}
