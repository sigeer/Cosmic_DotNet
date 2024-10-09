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
using Application.Core.Game.Skills;
using client;
using client.inventory;
using client.inventory.manipulator;
using client.status;
using constants.id;
using constants.inventory;
using constants.skills;
using net.packet;
using net.server;
using provider;
using server.life;
using server.maps;
using server.partyquest;
using tools;

namespace server;

/**
 * @author Matze
 * @author Frz
 * @author Ronan
 */
public class StatEffect
{
    private short watk, matk, wdef, mdef, acc, avoid, speed, jump;
    private short hp, mp;
    private double hpR, mpR;
    private short mhpRRate, mmpRRate, mobSkill, mobSkillLevel;
    private byte mhpR, mmpR;
    private short mpCon, hpCon;
    private int duration, target, barrier, mob;
    private bool overTime, repeatEffect;
    private int sourceid;
    private int moveTo;
    private int cp, nuffSkill;
    private List<Disease> cureDebuffs;
    private bool skill;
    private List<BuffStatValue> statups;
    private Dictionary<MonsterStatus, int> monsterStatus;
    private int x, y, mobCount, moneyCon, cooldown, morphId = 0, ghost, fatigue, berserk, booster;
    private double prop;
    private int itemCon, itemConNo;
    private int damage, attackCount, fixdamage;
    private Point? lt, rb;
    private short bulletCount, bulletConsume;
    private byte mapProtection;
    private CardItemupStats? cardStats;

    public class CardItemupStats
    {
        public int itemCode, prob;
        public bool party;
        public List<KeyValuePair<int, int>>? areas;

        public CardItemupStats(int code, int prob, List<KeyValuePair<int, int>>? areas, bool inParty)
        {
            this.itemCode = code;
            this.prob = prob;
            this.areas = areas;
            this.party = inParty;
        }

        public bool isInArea(int mapid)
        {
            if (this.areas == null)
            {
                return true;
            }

            foreach (var a in this.areas)
            {
                if (mapid >= a.Key && mapid <= a.Value)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private bool isEffectActive(int mapid, bool partyHunting)
    {
        if (cardStats == null)
        {
            return true;
        }

        if (!cardStats.isInArea(mapid))
        {
            return false;
        }

        return !cardStats.party || partyHunting;
    }

    public bool isActive(IPlayer applyto)
    {
        return isEffectActive(applyto.getMapId(), applyto.getPartyMembersOnSameMap().Count > 1);
    }

    public int getCardRate(int mapid, int itemid)
    {
        if (cardStats != null)
        {
            if (cardStats.itemCode == int.MaxValue)
            {
                return cardStats.prob;
            }
            else if (cardStats.itemCode < 1000)
            {
                if (itemid / 10000 == cardStats.itemCode)
                {
                    return cardStats.prob;
                }
            }
            else
            {
                if (itemid == cardStats.itemCode)
                {
                    return cardStats.prob;
                }
            }
        }

        return 0;
    }

    public static StatEffect loadSkillEffectFromData(Data? source, int skillid, bool overtime)
    {
        return new (source, skillid, true, overtime);
    }

    public static StatEffect loadItemEffectFromData(Data? source, int itemid)
    {
        return new (source, itemid, false, false);
    }

    private static void addBuffStatPairToListIfNotZero(List<BuffStatValue> list, BuffStat buffstat, int val)
    {
        if (val != 0)
        {
            list.Add(new(buffstat, val));
        }
    }

    private static byte GetMapProtection(int sourceid)
    {
        if (sourceid == ItemId.RED_BEAN_PORRIDGE || sourceid == ItemId.SOFT_WHITE_BUN)
        {
            return 1;   //elnath cold
        }
        else if (sourceid == ItemId.AIR_BUBBLE)
        {
            return 2;   //aqua road underwater
        }
        else
        {
            return 0;
        }
    }

    private StatEffect (Data? source, int sourceid, bool skill, bool overTime)
    {
        duration = DataTool.getIntConvert("time", source, -1);
        hp = (short)DataTool.getInt("hp", source, 0);
        hpR = DataTool.getInt("hpR", source, 0) / 100.0;
        mp = (short)DataTool.getInt("mp", source, 0);
        mpR = DataTool.getInt("mpR", source, 0) / 100.0;
        mpCon = (short)DataTool.getInt("mpCon", source, 0);
        hpCon = (short)DataTool.getInt("hpCon", source, 0);
        int iprop = DataTool.getInt("prop", source, 100);
        prop = iprop / 100.0;

        cp = DataTool.getInt("cp", source, 0);
        List<Disease> cure = new(5);
        if (DataTool.getInt("poison", source, 0) > 0)
        {
            cure.Add(Disease.POISON);
        }
        if (DataTool.getInt("seal", source, 0) > 0)
        {
            cure.Add(Disease.SEAL);
        }
        if (DataTool.getInt("darkness", source, 0) > 0)
        {
            cure.Add(Disease.DARKNESS);
        }
        if (DataTool.getInt("weakness", source, 0) > 0)
        {
            cure.Add(Disease.WEAKEN);
            cure.Add(Disease.SLOW);
        }
        if (DataTool.getInt("curse", source, 0) > 0)
        {
            cure.Add(Disease.CURSE);
        }
        cureDebuffs = cure;
        nuffSkill = DataTool.getInt("nuffSkill", source, 0);

        mobCount = DataTool.getInt("mobCount", source, 1);
        cooldown = DataTool.getInt("cooltime", source, 0);
        morphId = DataTool.getInt("morph", source, 0);
        ghost = DataTool.getInt("ghost", source, 0);
        fatigue = DataTool.getInt("incFatigue", source, 0);
        repeatEffect = DataTool.getInt("repeatEffect", source, 0) > 0;

        var mdd = source?.getChildByPath("0");
        if (mdd != null && mdd.getChildren().Count > 0)
        {
            mobSkill = (short)DataTool.getInt("mobSkill", mdd, 0);
            mobSkillLevel = (short)DataTool.getInt("level", mdd, 0);
            target = DataTool.getInt("target", mdd, 0);
        }
        else
        {
            mobSkill = 0;
            mobSkillLevel = 0;
            target = 0;
        }

        var mdds = source?.getChildByPath("mob");
        if (mdds != null)
        {
            if (mdds.getChildren().Count > 0)
            {
                mob = DataTool.getInt("mob", mdds, 0);
            }
        }
        this.sourceid = sourceid;
        this.skill = skill;
        if (!skill && duration > -1)
        {
            this.overTime = true;
        }
        else
        {
            duration *= 1000; // items have their times stored in ms, of course
            this.overTime = overTime;
        }

        statups = new();
        watk = (short)DataTool.getInt("pad", source, 0);
        wdef = (short)DataTool.getInt("pdd", source, 0);
        matk = (short)DataTool.getInt("mad", source, 0);
        mdef = (short)DataTool.getInt("mdd", source, 0);
        acc = (short)DataTool.getIntConvert("acc", source, 0);
        avoid = (short)DataTool.getInt("eva", source, 0);

        speed = (short)DataTool.getInt("speed", source, 0);
        jump = (short)DataTool.getInt("jump", source, 0);

        barrier = DataTool.getInt("barrier", source, 0);
        addBuffStatPairToListIfNotZero(statups, BuffStat.AURA, barrier);

        mapProtection = GetMapProtection(sourceid);
        addBuffStatPairToListIfNotZero(statups, BuffStat.MAP_PROTECTION, mapProtection);

        if (overTime && getSummonMovementType() == null)
        {
            if (!skill)
            {
                if (ItemId.isPyramidBuff(sourceid))
                {
                    berserk = DataTool.getInt("berserk", source, 0);
                    booster = DataTool.getInt("booster", source, 0);

                    addBuffStatPairToListIfNotZero(statups, BuffStat.BERSERK, berserk);
                    addBuffStatPairToListIfNotZero(statups, BuffStat.BOOSTER, booster);

                }
                else if (ItemId.isDojoBuff(sourceid) || isHpMpRecovery(sourceid))
                {
                    mhpR = (byte)DataTool.getInt("mhpR", source, 0);
                    mhpRRate = (short)(DataTool.getInt("mhpRRate", source, 0) * 100);
                    mmpR = (byte)DataTool.getInt("mmpR", source, 0);
                    mmpRRate = (short)(DataTool.getInt("mmpRRate", source, 0) * 100);

                    addBuffStatPairToListIfNotZero(statups, BuffStat.HPREC, mhpR);
                    addBuffStatPairToListIfNotZero(statups, BuffStat.MPREC, mmpR);

                }
                else if (ItemId.isRateCoupon(sourceid))
                {
                    switch (DataTool.getInt("expR", source, 0))
                    {
                        case 1:
                            addBuffStatPairToListIfNotZero(statups, BuffStat.COUPON_EXP1, 1);
                            break;

                        case 2:
                            addBuffStatPairToListIfNotZero(statups, BuffStat.COUPON_EXP2, 1);
                            break;

                        case 3:
                            addBuffStatPairToListIfNotZero(statups, BuffStat.COUPON_EXP3, 1);
                            break;

                        case 4:
                            addBuffStatPairToListIfNotZero(statups, BuffStat.COUPON_EXP4, 1);
                            break;
                    }

                    switch (DataTool.getInt("drpR", source, 0))
                    {
                        case 1:
                            addBuffStatPairToListIfNotZero(statups, BuffStat.COUPON_DRP1, 1);
                            break;

                        case 2:
                            addBuffStatPairToListIfNotZero(statups, BuffStat.COUPON_DRP2, 1);
                            break;

                        case 3:
                            addBuffStatPairToListIfNotZero(statups, BuffStat.COUPON_DRP3, 1);
                            break;
                    }
                }
                else if (ItemId.isMonsterCard(sourceid))
                {
                    int prob = 0, itemupCode = int.MaxValue;
                    List<KeyValuePair<int, int>>? areas = null;
                    bool inParty = false;

                    var con = source?.getChildByPath("con");
                    if (con != null)
                    {
                        areas = new(3);

                        foreach (Data conData in con.getChildren())
                        {
                            int type = DataTool.getInt("type", conData, -1);

                            if (type == 0)
                            {
                                int startMap = DataTool.getInt("sMap", conData, 0);
                                int endMap = DataTool.getInt("eMap", conData, 0);

                                areas.Add(new(startMap, endMap));
                            }
                            else if (type == 2)
                            {
                                inParty = true;
                            }
                        }

                        if (areas.Count == 0)
                        {
                            areas = null;
                        }
                    }

                    if (DataTool.getInt("mesoupbyitem", source, 0) != 0)
                    {
                        addBuffStatPairToListIfNotZero(statups, BuffStat.MESO_UP_BY_ITEM, 4);
                        prob = DataTool.getInt("prob", source, 1);
                    }

                    int itemupType = DataTool.getInt("itemupbyitem", source, 0);
                    if (itemupType != 0)
                    {
                        addBuffStatPairToListIfNotZero(statups, BuffStat.ITEM_UP_BY_ITEM, 4);
                        prob = DataTool.getInt("prob", source, 1);

                        switch (itemupType)
                        {
                            case 2:
                                itemupCode = DataTool.getInt("itemCode", source, 1);
                                break;

                            case 3:
                                itemupCode = DataTool.getInt("itemRange", source, 1);    // 3 digits
                                break;
                        }
                    }

                    if (DataTool.getInt("respectPimmune", source, 0) != 0)
                    {
                        addBuffStatPairToListIfNotZero(statups, BuffStat.RESPECT_PIMMUNE, 4);
                    }

                    if (DataTool.getInt("respectMimmune", source, 0) != 0)
                    {
                        addBuffStatPairToListIfNotZero(statups, BuffStat.RESPECT_MIMMUNE, 4);
                    }

                    if (DataTool.getString("defenseAtt", source) != null)
                    {
                        addBuffStatPairToListIfNotZero(statups, BuffStat.DEFENSE_ATT, 4);
                    }

                    if (DataTool.getString("defenseState", source) != null)
                    {
                        addBuffStatPairToListIfNotZero(statups, BuffStat.DEFENSE_STATE, 4);
                    }

                    int thaw = DataTool.getInt("thaw", source, 0);
                    if (thaw != 0)
                    {
                        addBuffStatPairToListIfNotZero(statups, BuffStat.MAP_PROTECTION, thaw > 0 ? 1 : 2);
                    }

                    cardStats = new CardItemupStats(itemupCode, prob, areas, inParty);
                }
                else if (ItemId.isExpIncrease(sourceid))
                {
                    addBuffStatPairToListIfNotZero(statups, BuffStat.EXP_INCREASE, DataTool.getInt("expinc", source, 0));
                }
            }
            else
            {
                if (isMapChair(sourceid))
                {
                    addBuffStatPairToListIfNotZero(statups, BuffStat.MAP_CHAIR, 1);
                }
                else if (YamlConfig.config.server.USE_ULTRA_NIMBLE_FEET
                    && (sourceid == Beginner.NIMBLE_FEET || sourceid == Noblesse.NIMBLE_FEET || sourceid == Evan.NIMBLE_FEET || sourceid == Legend.AGILE_BODY))
                {
                    jump = (short)(speed * 4);
                    speed *= 15;
                }
            }

            addBuffStatPairToListIfNotZero(statups, BuffStat.WATK, watk);
            addBuffStatPairToListIfNotZero(statups, BuffStat.WDEF, wdef);
            addBuffStatPairToListIfNotZero(statups, BuffStat.MATK, matk);
            addBuffStatPairToListIfNotZero(statups, BuffStat.MDEF, mdef);
            addBuffStatPairToListIfNotZero(statups, BuffStat.ACC, acc);
            addBuffStatPairToListIfNotZero(statups, BuffStat.AVOID, avoid);
            addBuffStatPairToListIfNotZero(statups, BuffStat.SPEED, speed);
            addBuffStatPairToListIfNotZero(statups, BuffStat.JUMP, jump);
        }

        var ltd = source?.getChildByPath("lt");
        if (ltd != null)
        {
            lt = (Point)ltd.getData()!;
            rb = (Point)source!.getChildByPath("rb")!.getData()!;

            if (YamlConfig.config.server.USE_MAXRANGE_ECHO_OF_HERO && (sourceid == Beginner.ECHO_OF_HERO || sourceid == Noblesse.ECHO_OF_HERO || sourceid == Legend.ECHO_OF_HERO || sourceid == Evan.ECHO_OF_HERO))
            {
                lt = new Point(int.MinValue, int.MinValue);
                rb = new Point(int.MaxValue, int.MaxValue);
            }
        }

        x = DataTool.getInt("x", source, 0);

        if ((sourceid == Beginner.RECOVERY || sourceid == Noblesse.RECOVERY || sourceid == Evan.RECOVERY || sourceid == Legend.RECOVERY) && YamlConfig.config.server.USE_ULTRA_RECOVERY == true)
        {
            x *= 10;
        }
        y = DataTool.getInt("y", source, 0);

        damage = DataTool.getIntConvert("damage", source, 100);
        fixdamage = DataTool.getIntConvert("fixdamage", source, -1);
        attackCount = DataTool.getIntConvert("attackCount", source, 1);
        bulletCount = (short)DataTool.getIntConvert("bulletCount", source, 1);
        bulletConsume = (short)DataTool.getIntConvert("bulletConsume", source, 0);
        moneyCon = DataTool.getIntConvert("moneyCon", source, 0);
        itemCon = DataTool.getInt("itemCon", source, 0);
        itemConNo = DataTool.getInt("itemConNo", source, 0);
        moveTo = DataTool.getInt("moveTo", source, -1);
        monsterStatus = new();
        if (skill)
        {
            switch (sourceid)
            {
                // BEGINNER
                case Beginner.RECOVERY:
                case Noblesse.RECOVERY:
                case Legend.RECOVERY:
                case Evan.RECOVERY:
                    statups.Add(new(BuffStat.RECOVERY, x));
                    break;
                case Beginner.ECHO_OF_HERO:
                case Noblesse.ECHO_OF_HERO:
                case Legend.ECHO_OF_HERO:
                case Evan.ECHO_OF_HERO:
                    statups.Add(new(BuffStat.ECHO_OF_HERO, x));
                    break;
                case Beginner.MONSTER_RIDER:
                case Noblesse.MONSTER_RIDER:
                case Legend.MONSTER_RIDER:
                case Corsair.BATTLE_SHIP:
                case Beginner.SPACESHIP:
                case Noblesse.SPACESHIP:
                case Beginner.YETI_MOUNT1:
                case Beginner.YETI_MOUNT2:
                case Noblesse.YETI_MOUNT1:
                case Noblesse.YETI_MOUNT2:
                case Legend.YETI_MOUNT1:
                case Legend.YETI_MOUNT2:
                case Beginner.WITCH_BROOMSTICK:
                case Noblesse.WITCH_BROOMSTICK:
                case Legend.WITCH_BROOMSTICK:
                case Beginner.BALROG_MOUNT:
                case Noblesse.BALROG_MOUNT:
                case Legend.BALROG_MOUNT:
                    statups.Add(new(BuffStat.MONSTER_RIDING, sourceid));
                    break;
                case Beginner.INVINCIBLE_BARRIER:
                case Noblesse.INVINCIBLE_BARRIER:
                case Legend.INVICIBLE_BARRIER:
                case Evan.INVINCIBLE_BARRIER:
                    statups.Add(new(BuffStat.DIVINE_BODY, 1));
                    break;
                case Fighter.POWER_GUARD:
                case Page.POWER_GUARD:
                    statups.Add(new(BuffStat.POWERGUARD, x));
                    break;
                case Spearman.HYPER_BODY:
                case GM.HYPER_BODY:
                case SuperGM.HYPER_BODY:
                    statups.Add(new(BuffStat.HYPERBODYHP, x));
                    statups.Add(new(BuffStat.HYPERBODYMP, y));
                    break;
                case Crusader.COMBO:
                case DawnWarrior.COMBO:
                    statups.Add(new(BuffStat.COMBO, 1));
                    break;
                case WhiteKnight.BW_FIRE_CHARGE:
                case WhiteKnight.BW_ICE_CHARGE:
                case WhiteKnight.BW_LIT_CHARGE:
                case WhiteKnight.SWORD_FIRE_CHARGE:
                case WhiteKnight.SWORD_ICE_CHARGE:
                case WhiteKnight.SWORD_LIT_CHARGE:
                case Paladin.BW_HOLY_CHARGE:
                case Paladin.SWORD_HOLY_CHARGE:
                case DawnWarrior.SOUL_CHARGE:
                case ThunderBreaker.LIGHTNING_CHARGE:
                    statups.Add(new(BuffStat.WK_CHARGE, x));
                    break;
                case DragonKnight.DRAGON_BLOOD:
                    statups.Add(new(BuffStat.DRAGONBLOOD, x));
                    break;
                case Hero.STANCE:
                case Paladin.STANCE:
                case DarkKnight.STANCE:
                case Aran.FREEZE_STANDING:
                    statups.Add(new(BuffStat.STANCE, iprop));
                    break;
                case DawnWarrior.FINAL_ATTACK:
                case WindArcher.FINAL_ATTACK:
                    statups.Add(new(BuffStat.FINALATTACK, x));
                    break;
                // MAGICIAN
                case Magician.MAGIC_GUARD:
                case BlazeWizard.MAGIC_GUARD:
                case Evan.MAGIC_GUARD:
                    statups.Add(new(BuffStat.MAGIC_GUARD, x));
                    break;
                case Cleric.INVINCIBLE:
                    statups.Add(new(BuffStat.INVINCIBLE, x));
                    break;
                case Priest.HOLY_SYMBOL:
                case SuperGM.HOLY_SYMBOL:
                    statups.Add(new(BuffStat.HOLY_SYMBOL, x));
                    break;
                case FPArchMage.INFINITY:
                case ILArchMage.INFINITY:
                case Bishop.INFINITY:
                    statups.Add(new(BuffStat.INFINITY, x));
                    break;
                case FPArchMage.MANA_REFLECTION:
                case ILArchMage.MANA_REFLECTION:
                case Bishop.MANA_REFLECTION:
                    statups.Add(new(BuffStat.MANA_REFLECTION, 1));
                    break;
                case Bishop.HOLY_SHIELD:
                    statups.Add(new(BuffStat.HOLY_SHIELD, x));
                    break;
                case BlazeWizard.ELEMENTAL_RESET:
                case Evan.ELEMENTAL_RESET:
                    statups.Add(new(BuffStat.ELEMENTAL_RESET, x));
                    break;
                case Evan.MAGIC_SHIELD:
                    statups.Add(new(BuffStat.MAGIC_SHIELD, x));
                    break;
                case Evan.MAGIC_RESISTANCE:
                    statups.Add(new(BuffStat.MAGIC_RESISTANCE, x));
                    break;
                case Evan.SLOW:
                    statups.Add(new(BuffStat.SLOW, x));
                    goto case Priest.MYSTIC_DOOR;
                // BOWMAN
                case Priest.MYSTIC_DOOR:
                case Hunter.SOUL_ARROW:
                case Crossbowman.SOUL_ARROW:
                case WindArcher.SOUL_ARROW:
                    statups.Add(new(BuffStat.SOULARROW, x));
                    break;
                case Ranger.PUPPET:
                case Sniper.PUPPET:
                case WindArcher.PUPPET:
                case Outlaw.OCTOPUS:
                case Corsair.WRATH_OF_THE_OCTOPI:
                    statups.Add(new(BuffStat.PUPPET, 1));
                    break;
                case Bowmaster.CONCENTRATE:
                    statups.Add(new(BuffStat.CONCENTRATE, x));
                    break;
                case Bowmaster.HAMSTRING:
                    statups.Add(new(BuffStat.HAMSTRING, x));
                    monsterStatus.AddOrUpdate(MonsterStatus.SPEED, x);
                    break;
                case Marksman.BLIND:
                    statups.Add(new(BuffStat.BLIND, x));
                    monsterStatus.AddOrUpdate(MonsterStatus.ACC, x);
                    break;
                case Bowmaster.SHARP_EYES:
                case Marksman.SHARP_EYES:
                    statups.Add(new(BuffStat.SHARP_EYES, x << 8 | y));
                    break;
                case WindArcher.WIND_WALK:
                    statups.Add(new(BuffStat.WIND_WALK, x));
                    goto case Rogue.DARK_SIGHT;
                //break;    thanks Vcoc for noticing WW not showing for other players when changing maps
                case Rogue.DARK_SIGHT:
                case NightWalker.DARK_SIGHT:
                    statups.Add(new(BuffStat.DARKSIGHT, x));
                    break;
                case Hermit.MESO_UP:
                    statups.Add(new(BuffStat.MESOUP, x));
                    break;
                case Hermit.SHADOW_PARTNER:
                case NightWalker.SHADOW_PARTNER:
                    statups.Add(new(BuffStat.SHADOWPARTNER, x));
                    break;
                case ChiefBandit.MESO_GUARD:
                    statups.Add(new(BuffStat.MESOGUARD, x));
                    break;
                case ChiefBandit.PICKPOCKET:
                    statups.Add(new(BuffStat.PICKPOCKET, x));
                    break;
                case NightLord.SHADOW_STARS:
                    statups.Add(new(BuffStat.SHADOW_CLAW, 0));
                    break;
                // PIRATE
                case Pirate.DASH:
                case ThunderBreaker.DASH:
                case Beginner.SPACE_DASH:
                case Noblesse.SPACE_DASH:
                    statups.Add(new(BuffStat.DASH2, x));
                    statups.Add(new(BuffStat.DASH, y));
                    break;
                case Corsair.SPEED_INFUSION:
                case Buccaneer.SPEED_INFUSION:
                case ThunderBreaker.SPEED_INFUSION:
                    statups.Add(new(BuffStat.SPEED_INFUSION, x));
                    break;
                case Outlaw.HOMING_BEACON:
                case Corsair.BULLSEYE:
                    statups.Add(new(BuffStat.HOMING_BEACON, x));
                    break;
                case ThunderBreaker.SPARK:
                    statups.Add(new(BuffStat.SPARK, x));
                    break;
                // MULTIPLE
                case Aran.POLEARM_BOOSTER:
                case Fighter.AXE_BOOSTER:
                case Fighter.SWORD_BOOSTER:
                case Page.BW_BOOSTER:
                case Page.SWORD_BOOSTER:
                case Spearman.POLEARM_BOOSTER:
                case Spearman.SPEAR_BOOSTER:
                case Hunter.BOW_BOOSTER:
                case Crossbowman.CROSSBOW_BOOSTER:
                case Assassin.CLAW_BOOSTER:
                case Bandit.DAGGER_BOOSTER:
                case FPMage.SPELL_BOOSTER:
                case ILMage.SPELL_BOOSTER:
                case Brawler.KNUCKLER_BOOSTER:
                case Gunslinger.GUN_BOOSTER:
                case DawnWarrior.SWORD_BOOSTER:
                case BlazeWizard.SPELL_BOOSTER:
                case WindArcher.BOW_BOOSTER:
                case NightWalker.CLAW_BOOSTER:
                case ThunderBreaker.KNUCKLER_BOOSTER:
                case Evan.MAGIC_BOOSTER:
                case Beginner.POWER_EXPLOSION:
                case Noblesse.POWER_EXPLOSION:
                case Legend.POWER_EXPLOSION:
                    statups.Add(new(BuffStat.BOOSTER, x));
                    break;
                case Hero.MAPLE_WARRIOR:
                case Paladin.MAPLE_WARRIOR:
                case DarkKnight.MAPLE_WARRIOR:
                case FPArchMage.MAPLE_WARRIOR:
                case ILArchMage.MAPLE_WARRIOR:
                case Bishop.MAPLE_WARRIOR:
                case Bowmaster.MAPLE_WARRIOR:
                case Marksman.MAPLE_WARRIOR:
                case NightLord.MAPLE_WARRIOR:
                case Shadower.MAPLE_WARRIOR:
                case Corsair.MAPLE_WARRIOR:
                case Buccaneer.MAPLE_WARRIOR:
                case Aran.MAPLE_WARRIOR:
                case Evan.MAPLE_WARRIOR:
                    statups.Add(new(BuffStat.MAPLE_WARRIOR, x));
                    break;
                // SUMMON
                case Ranger.SILVER_HAWK:
                case Sniper.GOLDEN_EAGLE:
                    statups.Add(new(BuffStat.SUMMON, 1));
                    monsterStatus.AddOrUpdate(MonsterStatus.STUN, 1);
                    break;
                case FPArchMage.ELQUINES:
                case Marksman.FROST_PREY:
                    statups.Add(new(BuffStat.SUMMON, 1));
                    monsterStatus.AddOrUpdate(MonsterStatus.FREEZE, 1);
                    break;
                case Priest.SUMMON_DRAGON:
                case Bowmaster.PHOENIX:
                case ILArchMage.IFRIT:
                case Bishop.BAHAMUT:
                case DarkKnight.BEHOLDER:
                case Outlaw.GAVIOTA:
                case DawnWarrior.SOUL:
                case BlazeWizard.FLAME:
                case WindArcher.STORM:
                case NightWalker.DARKNESS:
                case ThunderBreaker.LIGHTNING:
                case BlazeWizard.IFRIT:
                    statups.Add(new(BuffStat.SUMMON, 1));
                    break;
                // ----------------------------- MONSTER STATUS ---------------------------------- //
                case Crusader.ARMOR_CRASH:
                case DragonKnight.POWER_CRASH:
                case WhiteKnight.MAGIC_CRASH:
                    monsterStatus.AddOrUpdate(MonsterStatus.SEAL_SKILL, 1);
                    break;
                case Rogue.DISORDER:
                    monsterStatus.AddOrUpdate(MonsterStatus.WATK, x);
                    monsterStatus.AddOrUpdate(MonsterStatus.WDEF, y);
                    break;
                case Corsair.HYPNOTIZE:
                    monsterStatus.AddOrUpdate(MonsterStatus.INERTMOB, 1);
                    break;
                case NightLord.NINJA_AMBUSH:
                case Shadower.NINJA_AMBUSH:
                    monsterStatus.AddOrUpdate(MonsterStatus.NINJA_AMBUSH, damage);
                    break;
                case Page.THREATEN:
                    monsterStatus.AddOrUpdate(MonsterStatus.WATK, x);
                    monsterStatus.AddOrUpdate(MonsterStatus.WDEF, y);
                    break;
                case DragonKnight.DRAGON_ROAR:
                    hpR = -x / 100.0;
                    monsterStatus.AddOrUpdate(MonsterStatus.STUN, 1);
                    break;
                case Crusader.AXE_COMA:
                case Crusader.SWORD_COMA:
                case Crusader.SHOUT:
                case WhiteKnight.CHARGE_BLOW:
                case Hunter.ARROW_BOMB:
                case ChiefBandit.ASSAULTER:
                case Shadower.BOOMERANG_STEP:
                case Brawler.BACK_SPIN_BLOW:
                case Brawler.DOUBLE_UPPERCUT:
                case Buccaneer.DEMOLITION:
                case Buccaneer.SNATCH:
                case Buccaneer.BARRAGE:
                case Gunslinger.BLANK_SHOT:
                case DawnWarrior.COMA:
                case ThunderBreaker.BARRAGE:
                case Aran.ROLLING_SPIN:
                case Evan.FIRE_BREATH:
                case Evan.BLAZE:
                    monsterStatus.AddOrUpdate(MonsterStatus.STUN, 1);
                    break;
                case NightLord.TAUNT:
                case Shadower.TAUNT:
                    monsterStatus.AddOrUpdate(MonsterStatus.SHOWDOWN, x);
                    monsterStatus.AddOrUpdate(MonsterStatus.MDEF, x);
                    monsterStatus.AddOrUpdate(MonsterStatus.WDEF, x);
                    break;
                case ILWizard.COLD_BEAM:
                case ILMage.ICE_STRIKE:
                case ILArchMage.BLIZZARD:
                case ILMage.ELEMENT_COMPOSITION:
                case Sniper.BLIZZARD:
                case Outlaw.ICE_SPLITTER:
                case FPArchMage.PARALYZE:
                case Aran.COMBO_TEMPEST:
                case Evan.ICE_BREATH:
                    monsterStatus.AddOrUpdate(MonsterStatus.FREEZE, 1);
                    duration *= 2; // freezing skills are a little strange
                    break;
                case FPWizard.SLOW:
                case ILWizard.SLOW:
                case BlazeWizard.SLOW:
                    monsterStatus.AddOrUpdate(MonsterStatus.SPEED, x);
                    break;
                case FPWizard.POISON_BREATH:
                case FPMage.ELEMENT_COMPOSITION:
                    monsterStatus.AddOrUpdate(MonsterStatus.POISON, 1);
                    break;
                case Priest.DOOM:
                    monsterStatus.AddOrUpdate(MonsterStatus.DOOM, 1);
                    break;
                case ILMage.SEAL:
                case FPMage.SEAL:
                case BlazeWizard.SEAL:
                    monsterStatus.AddOrUpdate(MonsterStatus.SEAL, 1);
                    break;
                case Hermit.SHADOW_WEB: // shadow web
                case NightWalker.SHADOW_WEB:
                    monsterStatus.AddOrUpdate(MonsterStatus.SHADOW_WEB, 1);
                    break;
                case FPArchMage.FIRE_DEMON:
                case ILArchMage.ICE_DEMON:
                    monsterStatus.AddOrUpdate(MonsterStatus.POISON, 1);
                    monsterStatus.AddOrUpdate(MonsterStatus.FREEZE, 1);
                    break;
                case Evan.PHANTOM_IMPRINT:
                    monsterStatus.AddOrUpdate(MonsterStatus.PHANTOM_IMPRINT, x);
                    goto case Aran.COMBO_ABILITY;
                //ARAN
                case Aran.COMBO_ABILITY:
                    statups.Add(new(BuffStat.ARAN_COMBO, 100));
                    break;
                case Aran.COMBO_BARRIER:
                    statups.Add(new(BuffStat.COMBO_BARRIER, x));
                    break;
                case Aran.COMBO_DRAIN:
                    statups.Add(new(BuffStat.COMBO_DRAIN, x));
                    break;
                case Aran.SMART_KNOCKBACK:
                    statups.Add(new(BuffStat.SMART_KNOCKBACK, x));
                    break;
                case Aran.BODY_PRESSURE:
                    statups.Add(new(BuffStat.BODY_PRESSURE, x));
                    break;
                case Aran.SNOW_CHARGE:
                    statups.Add(new(BuffStat.WK_CHARGE, duration));
                    break;
                default:
                    break;
            }
        }
        if (isMorph())
        {
            statups.Add(new(BuffStat.MORPH, getMorph()));
        }
        if (ghost > 0 && !skill)
        {
            statups.Add(new(BuffStat.GHOST_MORPH, ghost));
        }
        statups.TrimExcess();
    }

    /**
     * @param applyto
     * @param obj
     * @param attack  damage done by the skill
     */
    public void applyPassive(IPlayer applyto, IMapObject obj, int attack)
    {
        if (makeChanceResult())
        {
            switch (sourceid)
            { // MP eater
                case FPWizard.MP_EATER:
                case ILWizard.MP_EATER:
                case Cleric.MP_EATER:
                    if (obj == null || obj.getType() != MapObjectType.MONSTER)
                    {
                        return;
                    }
                    Monster mob = (Monster)obj; // x is absorb percentage
                    if (!mob.isBoss())
                    {
                        int absorbMp = Math.Min((int)(mob.getMaxMp() * (getX() / 100.0)), mob.getMp());
                        if (absorbMp > 0)
                        {
                            mob.setMp(mob.getMp() - absorbMp);
                            applyto.addMP(absorbMp);
                            applyto.sendPacket(PacketCreator.showOwnBuffEffect(sourceid, 1));
                            applyto.getMap().broadcastMessage(applyto, PacketCreator.showBuffEffect(applyto.getId(), sourceid, 1), false);
                        }
                    }
                    break;
            }
        }
    }

    public bool applyEchoOfHero(IPlayer applyfrom)
    {
        Dictionary<int, IPlayer> mapPlayers = applyfrom.getMap().getMapPlayers();
        mapPlayers.Remove(applyfrom.getId());

        bool hwResult = applyTo(applyfrom);
        foreach (IPlayer chr in mapPlayers.Values)
        {    // Echo of Hero not buffing players in the map detected thanks to Masterrulax
            applyTo(applyfrom, chr, false, null, false, 1);
        }

        return hwResult;
    }

    public bool applyTo(IPlayer chr)
    {
        return applyTo(chr, chr, true, null, false, 1);
    }

    public bool applyTo(IPlayer chr, bool useMaxRange)
    {
        return applyTo(chr, chr, true, null, useMaxRange, 1);
    }

    public bool applyTo(IPlayer chr, Point pos)
    {
        return applyTo(chr, chr, true, pos, false, 1);
    }

    // primary: the player caster of the buff
    private bool applyTo(IPlayer applyfrom, IPlayer applyto, bool primary, Point? pos, bool useMaxRange, int affectedPlayers)
    {
        if (skill && (sourceid == GM.HIDE || sourceid == SuperGM.HIDE))
        {
            applyto.toggleHide(false);
            return true;
        }

        if (primary && isHeal())
        {
            affectedPlayers = applyBuff(applyfrom, useMaxRange);
        }

        int hpchange = calcHPChange(applyfrom, primary, affectedPlayers);
        int mpchange = calcMPChange(applyfrom, primary);
        if (primary)
        {
            if (itemConNo != 0)
            {
                if (!applyto.getAbstractPlayerInteraction().hasItem(itemCon, itemConNo))
                {
                    applyto.sendPacket(PacketCreator.enableActions());
                    return false;
                }
                InventoryManipulator.removeById(applyto.getClient(), ItemConstants.getInventoryType(itemCon), itemCon, itemConNo, false, true);
            }
        }
        else
        {
            if (isResurrection())
            {
                hpchange = applyto.getCurrentMaxHp();
                applyto.broadcastStance(applyto.isFacingLeft() ? 5 : 4);
            }
        }

        if (isDispel() && makeChanceResult())
        {
            applyto.dispelDebuffs();
        }
        else if (isCureAllAbnormalStatus())
        {
            applyto.purgeDebuffs();
        }
        else if (isComboReset())
        {
            applyto.setCombo(0);
        }
        /*if (applyfrom.getMp() < getMpCon()) {
         AutobanFactory.MPCON.addPoint(applyfrom.getAutobanManager(), "mpCon hack for skill:" + sourceid + "; Player MP: " + applyto.getMp() + " MP Needed: " + getMpCon());
         } */

        if (!applyto.applyHpMpChange(hpCon, hpchange, mpchange))
        {
            applyto.sendPacket(PacketCreator.enableActions());
            return false;
        }

        if (moveTo != -1)
        {
            if (moveTo != applyto.getMapId())
            {
                IMap target;
                Portal pt;

                if (moveTo == MapId.NONE)
                {
                    target = applyto.getMap().getReturnMap();
                    pt = target.getRandomPlayerSpawnpoint();
                }
                else
                {
                    target = applyto.getClient().getWorldServer().getChannel(applyto.getClient().getChannel()).getMapFactory().getMap(moveTo);
                    int targetid = target.getId() / 10000000;
                    if (targetid != 60 
                        && applyto.getMapId() / 10000000 != 61 
                        && targetid != applyto.getMapId() / 10000000 
                        && targetid != 21 
                        && targetid != 20 
                        && targetid != 12 
                        && (applyto.getMapId() / 10000000 != 10 
                        && applyto.getMapId() / 10000000 != 12))
                    {
                        return false;
                    }

                    pt = target.getRandomPlayerSpawnpoint();
                }

                applyto.changeMap(target, pt);
            }
            else
            {
                return false;
            }
        }
        if (isShadowClaw())
        {
            short projectileConsume = this.getBulletConsume();  // noticed by shavit

            Inventory use = applyto.getInventory(InventoryType.USE);
            use.lockInventory();
            try
            {
                Item? projectile = null;
                for (int i = 1; i <= use.getSlotLimit(); i++)
                { // impose order...
                    var item = use.getItem((short)i);
                    if (item != null)
                    {
                        if (ItemConstants.isThrowingStar(item.getItemId()) && item.getQuantity() >= projectileConsume)
                        {
                            projectile = item;
                            break;
                        }
                    }
                }
                if (projectile == null)
                {
                    return false;
                }
                else
                {
                    InventoryManipulator.removeFromSlot(applyto.getClient(), InventoryType.USE, projectile.getPosition(), projectileConsume, false, true);
                }
            }
            finally
            {
                use.unlockInventory();
            }
        }
        var summonMovementType = getSummonMovementType();
        if (overTime || isCygnusFA() || summonMovementType != null)
        {
            if (summonMovementType != null && pos != null)
            {
                applyto.cancelBuffStats(summonMovementType.Value == SummonMovementType.STATIONARY ? BuffStat.PUPPET : BuffStat.SUMMON);
                applyto.sendPacket(PacketCreator.enableActions());
            }

            applyBuffEffect(applyfrom, applyto, primary);
        }

        if (primary)
        {
            if (overTime)
            {
                applyBuff(applyfrom, useMaxRange);
            }

            if (isMonsterBuff())
            {
                applyMonsterBuff(applyfrom);
            }
        }

        if (applyto.MountModel != null && this.getFatigue() != 0)
        {
            applyto.MountModel.setTiredness(applyto.MountModel.getTiredness() + this.getFatigue());
        }

        if (summonMovementType != null && pos != null)
        {
            Summon tosummon = new Summon(applyfrom, sourceid, pos.Value, summonMovementType.Value);
            applyfrom.getMap().spawnSummon(tosummon);
            applyfrom.addSummon(sourceid, tosummon);
            tosummon.addHP(x);
            if (isBeholder())
            {
                tosummon.addHP(1);
            }
        }
        if (isMagicDoor() && !FieldLimit.DOOR.check(applyto.getMap().getFieldLimit()))
        { 
            // Magic Door
            int y = applyto.getFh();
            if (y == 0)
            {
                y = applyto.getMap().getGroundBelow(applyto.getPosition()).Y;    // thanks Lame for pointing out unusual cases of doors sending players on ground below
            }
            Point doorPosition = new Point(applyto.getPosition().X, y);
            Door door = new Door(applyto, doorPosition);

            if (door.getOwnerId() >= 0)
            {
                applyto.applyPartyDoor(door, false);

                door.getTarget().spawnDoor(door.getAreaDoor());
                door.getTown().spawnDoor(door.getTownDoor());
            }
            else
            {
                InventoryManipulator.addFromDrop(applyto.getClient(), new Item(ItemId.MAGIC_ROCK, 0, 1), false);

                if (door.getOwnerId() == -3)
                {
                    applyto.dropMessage(5, "Mystic Door cannot be cast far from a spawn point. Nearest one is at " + door.getDoorStatus().Value.Value + "pts " + door.getDoorStatus().Value.Key);
                }
                else if (door.getOwnerId() == -2)
                {
                    applyto.dropMessage(5, "Mystic Door cannot be cast on a slope, try elsewhere.");
                }
                else
                {
                    applyto.dropMessage(5, "There are no door portals available for the town at this moment. Try again later.");
                }

                applyto.cancelBuffStats(BuffStat.SOULARROW);  // cancel door buff
            }
        }
        else if (isMist())
        {
            Rectangle bounds = calculateBoundingBox(sourceid == NightWalker.POISON_BOMB ? pos.Value : applyfrom.getPosition(), applyfrom.isFacingLeft());
            var mist = new PlayerMist(bounds, applyfrom, this);
            applyfrom.getMap().spawnMist(mist, getDuration(), mist.isPoisonMist(), false, mist.isRecoveryMist());
        }
        else if (isTimeLeap())
        {
            applyto.removeAllCooldownsExcept(Buccaneer.TIME_LEAP, true);
        }
        else if (cp != 0 && applyto.getMonsterCarnival() != null)
        {
            applyto.gainCP(cp);
        }
        else if (nuffSkill != 0 && applyto.getParty() != null && applyto.getMap().isCPQMap())
        { 
            // added by Drago (Dragohe4rt)
            var skill = CarnivalFactory.getInstance().getSkill(nuffSkill);
            if (skill != null)
            {
                var dis = skill.getDisease();
                var opposition = applyfrom.getParty()!.getEnemy()!;
                if (skill.targetsAll)
                {
                    foreach (var chrApp in opposition.getMembers())
                    {
                        if (chrApp != null && chrApp.getMap().isCPQMap())
                        {
                            if (dis == null)
                            {
                                chrApp.dispel();
                            }
                            else
                            {
                                MobSkill mobSkill = MobSkillFactory.getMobSkillOrThrow(dis.getMobSkillType()!.Value, skill.level);
                                chrApp.giveDebuff(dis, mobSkill);
                            }
                        }
                    }
                }
                else
                {
                    var chrApp = applyfrom.getMap().getCharacterById(opposition.GetRandomMember().getId());
                    if (chrApp != null && chrApp.getMap().isCPQMap())
                    {
                        if (dis == null)
                        {
                            chrApp.dispel();
                        }
                        else
                        {
                            MobSkill mobSkill = MobSkillFactory.getMobSkillOrThrow(dis.getMobSkillType()!.Value, skill.level);
                            chrApp.giveDebuff(dis, mobSkill);
                        }
                    }
                }
            }
        }
        else if (cureDebuffs.Count > 0)
        { // added by Drago (Dragohe4rt)
            foreach (Disease debuff in cureDebuffs)
            {
                applyfrom.dispelDebuff(debuff);
            }
        }
        else if (mobSkill > 0 && mobSkillLevel > 0)
        {
            MobSkillType mobSkillType = MobSkillTypeUtils.from(mobSkill);
            MobSkill ms = MobSkillFactory.getMobSkillOrThrow(mobSkillType, mobSkillLevel);
            var dis = Disease.GetBySkillTrust(mobSkillType);

            if (target > 0)
            {
                foreach (IPlayer chr in applyto.getMap().getAllPlayers())
                {
                    if (chr.getId() != applyto.getId())
                    {
                        chr.giveDebuff(dis, ms);
                    }
                }
            }
            else
            {
                applyto.giveDebuff(dis, ms);
            }
        }
        return true;
    }

    private int applyBuff(IPlayer applyfrom, bool useMaxRange)
    {
        int affectedc = 1;

        if (isPartyBuff() && (applyfrom.getParty() != null || isGmBuff()))
        {
            Rectangle bounds = (!useMaxRange) ? calculateBoundingBox(applyfrom.getPosition(), applyfrom.isFacingLeft()) : new Rectangle(int.MinValue / 2, int.MinValue / 2, int.MaxValue, int.MaxValue);
            List<IMapObject> affecteds = applyfrom.getMap().getMapObjectsInRect(bounds, Arrays.asList(MapObjectType.PLAYER));
            List<IPlayer> affectedp = new(affecteds.Count);
            foreach (var affectedmo in affecteds)
            {
                IPlayer affected = (IPlayer)affectedmo;
                if (affected != applyfrom && (isGmBuff() || (applyfrom.getParty()?.Equals(affected.getParty()) ?? false)))
                {
                    if (!isResurrection())
                    {
                        if (affected.isAlive())
                        {
                            affectedp.Add(affected);
                        }
                    }
                    else
                    {
                        if (!affected.isAlive())
                        {
                            affectedp.Add(affected);
                        }
                    }
                }
            }

            affectedc += affectedp.Count;   // used for heal
            foreach (IPlayer affected in affectedp)
            {
                applyTo(applyfrom, affected, false, null, useMaxRange, affectedc);
                affected.sendPacket(PacketCreator.showOwnBuffEffect(sourceid, 2));
                affected.getMap().broadcastMessage(affected, PacketCreator.showBuffEffect(affected.getId(), sourceid, 2), false);
            }
        }

        return affectedc;
    }

    private void applyMonsterBuff(IPlayer applyfrom)
    {
        Rectangle bounds = calculateBoundingBox(applyfrom.getPosition(), applyfrom.isFacingLeft());
        List<IMapObject> affected = applyfrom.getMap().getMapObjectsInRect(bounds, Arrays.asList(MapObjectType.MONSTER));
        var skill_ = SkillFactory.GetSkillTrust(sourceid);
        int i = 0;
        foreach (var mo in affected)
        {
            Monster monster = (Monster)mo;
            if (isDispel())
            {
                monster.debuffMob(skill_.getId());
            }
            else if (isSeal() && monster.isBoss())
            {  // thanks IxianMace for noticing seal working on bosses
                // do nothing
            }
            else
            {
                if (makeChanceResult())
                {
                    monster.applyStatus(applyfrom, new MonsterStatusEffect(getMonsterStati(), skill_), isPoison(), getDuration());
                    if (isCrash())
                    {
                        monster.debuffMob(skill_.getId());
                    }
                }
            }
            i++;
            if (i >= mobCount)
            {
                break;
            }
        }
    }

    private Rectangle calculateBoundingBox(Point posFrom, bool facingLeft)
    {
        Point mylt;
        Point myrb;
        if (facingLeft)
        {
            mylt = new Point(lt.Value.X + posFrom.X, lt.Value.Y + posFrom.Y);
            myrb = new Point(rb.Value.X + posFrom.X, rb.Value.Y + posFrom.Y);
        }
        else
        {
            myrb = new Point(-lt.Value.X + posFrom.X, rb.Value.Y + posFrom.Y);  // thanks Conrad, April for noticing a disturbance in AoE skill behavior after a hitched refactor here
            mylt = new Point(-rb.Value.X + posFrom.X, lt.Value.Y + posFrom.Y);
        }
        Rectangle bounds = new Rectangle(mylt.X, mylt.Y, myrb.X - mylt.X, myrb.Y - mylt.Y);
        return bounds;
    }

    public int getBuffLocalDuration()
    {
        return !YamlConfig.config.server.USE_BUFF_EVERLASTING ? duration : int.MaxValue;
    }

    public void silentApplyBuff(IPlayer chr, long localStartTime)
    {
        int localDuration = getBuffLocalDuration();
        localDuration = alchemistModifyVal(chr, localDuration, false);
        //CancelEffectAction cancelAction = new CancelEffectAction(chr, this, starttime);
        //ScheduledFuture<?> schedule = TimerManager.getInstance().schedule(cancelAction, ((starttime + localDuration) - Server.getInstance().getCurrentTime()));

        chr.registerEffect(this, localStartTime, localStartTime + localDuration, true);
        SummonMovementType? summonMovementType = getSummonMovementType();
        if (summonMovementType != null)
        {
            Summon tosummon = new Summon(chr, sourceid, chr.getPosition(), summonMovementType.Value);
            if (!tosummon.isStationary())
            {
                chr.addSummon(sourceid, tosummon);
                tosummon.addHP(x);
            }
        }
        if (sourceid == Corsair.BATTLE_SHIP)
        {
            chr.announceBattleshipHp();
        }
    }

    public void applyComboBuff(IPlayer applyto, int combo)
    {
        applyto.sendPacket(PacketCreator.giveBuff(sourceid, 99999, new BuffStatValue(BuffStat.ARAN_COMBO, combo)));

        long starttime = Server.getInstance().getCurrentTime();
        //	CancelEffectAction cancelAction = new CancelEffectAction(applyto, this, starttime);
        //	ScheduledFuture<?> schedule = TimerManager.getInstance().schedule(cancelAction, ((starttime + 99999) - Server.getInstance().getCurrentTime()));
        applyto.registerEffect(this, starttime, long.MaxValue, false);
    }

    public void applyBeaconBuff(IPlayer applyto, int objectid)
    { 
        // thanks Thora & Hyun for reporting an issue with homing beacon autoflagging mobs when changing maps
        applyto.sendPacket(PacketCreator.giveBuff(1, sourceid, new BuffStatValue(BuffStat.HOMING_BEACON, objectid)));

        long starttime = Server.getInstance().getCurrentTime();
        applyto.registerEffect(this, starttime, long.MaxValue, false);
    }

    public void updateBuffEffect(IPlayer target, BuffStatValue[] activeStats, long starttime)
    {
        int localDuration = getBuffLocalDuration();
        localDuration = alchemistModifyVal(target, localDuration, false);

        long leftDuration = (starttime + localDuration) - Server.getInstance().getCurrentTime();
        if (leftDuration > 0)
        {
            if (isDash() || isInfusion())
            {
                target.sendPacket(PacketCreator.givePirateBuff(activeStats, (skill ? sourceid : -sourceid), (int)leftDuration));
            }
            else
            {
                target.sendPacket(PacketCreator.giveBuff((skill ? sourceid : -sourceid), (int)leftDuration, activeStats));
            }
        }
    }

    private void applyBuffEffect(IPlayer applyfrom, IPlayer applyto, bool primary)
    {
        if (!isMonsterRiding() && !isCouponBuff() && !isMysticDoor() && !isHyperBody() && !isCombo())
        {     // last mystic door already dispelled if it has been used before.
            applyto.cancelEffect(this, true, -1);
        }

        BuffStatValue[] localstatups = statups.ToArray();
        int localDuration = getBuffLocalDuration();
        int localsourceid = sourceid;
        int seconds = localDuration / 1000;
        IMount? givemount = null;
        if (isMonsterRiding())
        {
            int ridingMountId = 0;
            Item? mount = applyfrom.getInventory(InventoryType.EQUIPPED).getItem(-18);
            if (mount != null)
            {
                ridingMountId = mount.getItemId();
            }

            if (sourceid == Corsair.BATTLE_SHIP)
            {
                ridingMountId = ItemId.BATTLESHIP;
            }
            else if (sourceid == Beginner.SPACESHIP || sourceid == Noblesse.SPACESHIP)
            {
                ridingMountId = 1932000 + applyto.getSkillLevel(sourceid);
            }
            else if (sourceid == Beginner.YETI_MOUNT1 || sourceid == Noblesse.YETI_MOUNT1 || sourceid == Legend.YETI_MOUNT1)
            {
                ridingMountId = 1932003;
            }
            else if (sourceid == Beginner.YETI_MOUNT2 || sourceid == Noblesse.YETI_MOUNT2 || sourceid == Legend.YETI_MOUNT2)
            {
                ridingMountId = 1932004;
            }
            else if (sourceid == Beginner.WITCH_BROOMSTICK || sourceid == Noblesse.WITCH_BROOMSTICK || sourceid == Legend.WITCH_BROOMSTICK)
            {
                ridingMountId = 1932005;
            }
            else if (sourceid == Beginner.BALROG_MOUNT || sourceid == Noblesse.BALROG_MOUNT || sourceid == Legend.BALROG_MOUNT)
            {
                ridingMountId = 1932010;
            }

            // thanks inhyuk for noticing some skill mounts not acting properly for other players when changing maps
            givemount = applyto.mount(ridingMountId, sourceid);
            applyto.getClient().getWorldServer().registerMountHunger(applyto);

            localDuration = sourceid;
            localsourceid = ridingMountId;
            localstatups = [new BuffStatValue(BuffStat.MONSTER_RIDING, 0)];
        }
        else if (isSkillMorph())
        {
            for (int i = 0; i < localstatups.Length; i++)
            {
                if (localstatups[i].BuffState.Equals(BuffStat.MORPH))
                {
                    localstatups[i] = new(BuffStat.MORPH, getMorph(applyto));
                    break;
                }
            }
        }
        if (primary)
        {
            localDuration = alchemistModifyVal(applyfrom, localDuration, false);
            applyto.getMap().broadcastMessage(applyto, PacketCreator.showBuffEffect(applyto.getId(), sourceid, 1, 3), false);
        }
        if (localstatups.Length > 0)
        {
            Packet? buff = null;
            Packet? mbuff = null;
            if (this.isActive(applyto))
            {
                buff = PacketCreator.giveBuff((skill ? sourceid : -sourceid), localDuration, localstatups);
            }
            if (isDash())
            {
                buff = PacketCreator.givePirateBuff(statups, sourceid, seconds);
                mbuff = PacketCreator.giveForeignPirateBuff(applyto.getId(), sourceid, seconds, localstatups);
            }
            else if (isWkCharge())
            {
                mbuff = PacketCreator.giveForeignWKChargeEffect(applyto.getId(), sourceid, localstatups);
            }
            else if (isInfusion())
            {
                buff = PacketCreator.givePirateBuff(localstatups, sourceid, seconds);
                mbuff = PacketCreator.giveForeignPirateBuff(applyto.getId(), sourceid, seconds, localstatups);
            }
            else if (isDs())
            {
                mbuff = PacketCreator.giveForeignBuff(applyto.getId(), new BuffStatValue(BuffStat.DARKSIGHT, 0));
            }
            else if (isWw())
            {
                List<KeyValuePair<BuffStat, int>> dsstat = Collections.singletonList(new KeyValuePair<BuffStat, int>());
                mbuff = PacketCreator.giveForeignBuff(applyto.getId(), new BuffStatValue(BuffStat.WIND_WALK, 0));
            }
            else if (isCombo())
            {
                int comboCount = applyto.getBuffedValue(BuffStat.COMBO) ?? 0;

                var combo = new BuffStatValue(BuffStat.COMBO, comboCount);
                buff = PacketCreator.giveBuff((skill ? sourceid : -sourceid), localDuration, combo);
                mbuff = PacketCreator.giveForeignBuff(applyto.getId(), combo);
            }
            else if (isMonsterRiding())
            {
                if (sourceid == Corsair.BATTLE_SHIP)
                {//hp
                    if (applyto.getBattleshipHp() <= 0)
                    {
                        applyto.resetBattleshipHp();
                    }

                    localstatups = statups.ToArray();
                }
                buff = PacketCreator.giveBuff(localsourceid, localDuration, localstatups);
                mbuff = PacketCreator.showMonsterRiding(applyto.getId(), givemount!);
                localDuration = duration;
            }
            else if (isShadowPartner())
            {
                mbuff = PacketCreator.giveForeignBuff(applyto.getId(), new BuffStatValue(BuffStat.SHADOWPARTNER, 0));
            }
            else if (isSoulArrow())
            {
                mbuff = PacketCreator.giveForeignBuff(applyto.getId(), new BuffStatValue(BuffStat.SOULARROW, 0));
            }
            else if (isEnrage())
            {
                applyto.handleOrbconsume();
            }
            else if (isMorph())
            {
                mbuff = PacketCreator.giveForeignBuff(applyto.getId(), new BuffStatValue(BuffStat.MORPH, getMorph(applyto)));
            }
            else if (isAriantShield())
            {
                mbuff = PacketCreator.giveForeignBuff(applyto.getId(), new BuffStatValue(BuffStat.AURA, 1));
            }

            if (buff != null)
            {
                //Thanks flav for such a simple release! :)
                //Thanks Conrad, Atoot for noticing summons not using buff icon

                applyto.sendPacket(buff);
            }

            long starttime = Server.getInstance().getCurrentTime();
            //CancelEffectAction cancelAction = new CancelEffectAction(applyto, this, starttime);
            //ScheduledFuture<?> schedule = TimerManager.getInstance().schedule(cancelAction, localDuration);
            applyto.registerEffect(this, starttime, starttime + localDuration, false);
            if (mbuff != null)
            {
                applyto.getMap().broadcastMessage(applyto, mbuff, false);
            }
            if (sourceid == Corsair.BATTLE_SHIP)
            {
                applyto.announceBattleshipHp();
            }
        }
    }

    private int calcHPChange(IPlayer applyfrom, bool primary, int affectedPlayers)
    {
        int hpchange = 0;
        if (hp != 0)
        {
            if (!skill)
            {
                if (primary)
                {
                    hpchange += alchemistModifyVal(applyfrom, hp, true);
                }
                else
                {
                    hpchange += hp;
                }
                if (applyfrom.hasDisease(Disease.ZOMBIFY))
                {
                    hpchange /= 2;
                }
            }
            else
            { // assumption: this is heal
                float hpHeal = (applyfrom.getCurrentMaxHp() * (float)hp / (100.0f * affectedPlayers));
                hpchange += (int)hpHeal;
                if (applyfrom.hasDisease(Disease.ZOMBIFY))
                {
                    hpchange = -hpchange;
                    hpCon = 0;
                }
            }
        }
        if (hpR != 0)
        {
            hpchange += (int)(applyfrom.getCurrentMaxHp() * hpR) / (applyfrom.hasDisease(Disease.ZOMBIFY) ? 2 : 1);
        }
        if (primary)
        {
            if (hpCon != 0)
            {
                hpchange -= hpCon;
            }
        }
        if (isChakra())
        {
            hpchange += makeHealHP(getY() / 100.0, applyfrom.getTotalLuk(), 2.3, 3.5);
        }
        else if (sourceid == SuperGM.HEAL_PLUS_DISPEL)
        {
            hpchange += applyfrom.getCurrentMaxHp();
        }

        return hpchange;
    }

    private int makeHealHP(double rate, double stat, double lowerfactor, double upperfactor)
    {
        return (int)((Randomizer.nextDouble() * ((int)(stat * upperfactor * rate) - (int)(stat * lowerfactor * rate) + 1)) + (int)(stat * lowerfactor * rate));
    }

    private int calcMPChange(IPlayer applyfrom, bool primary)
    {
        int mpchange = 0;
        if (mp != 0)
        {
            if (primary)
            {
                mpchange += alchemistModifyVal(applyfrom, mp, true);
            }
            else
            {
                mpchange += mp;
            }
        }
        if (mpR != 0)
        {
            mpchange += (int)(applyfrom.getCurrentMaxMp() * mpR);
        }
        if (primary)
        {
            if (mpCon != 0)
            {
                double mod = 1.0;
                bool isAFpMage = applyfrom.getJob().isA(Job.FP_MAGE);
                bool isCygnus = applyfrom.getJob().isA(Job.BLAZEWIZARD2);
                bool isEvan = applyfrom.getJob().isA(Job.EVAN7);
                if (isAFpMage || isCygnus || isEvan || applyfrom.getJob().isA(Job.IL_MAGE))
                {
                    Skill amp = isAFpMage ? SkillFactory.GetSkillTrust(FPMage.ELEMENT_AMPLIFICATION) : (isCygnus ? SkillFactory.GetSkillTrust(BlazeWizard.ELEMENT_AMPLIFICATION) : (isEvan ? SkillFactory.GetSkillTrust(Evan.MAGIC_AMPLIFICATION) : SkillFactory.GetSkillTrust(ILMage.ELEMENT_AMPLIFICATION)));
                    int ampLevel = applyfrom.getSkillLevel(amp);
                    if (ampLevel > 0)
                    {
                        mod = amp.getEffect(ampLevel).getX() / 100.0;
                    }
                }
                mpchange -= (int)(mpCon * mod);
                if (applyfrom.getBuffedValue(BuffStat.INFINITY) != null)
                {
                    mpchange = 0;
                }
                else if (applyfrom.getBuffedValue(BuffStat.CONCENTRATE) != null)
                {
                    mpchange -= (int)(mpchange * ((applyfrom.getBuffedValue(BuffStat.CONCENTRATE) ?? 0) / 100));
                }
            }
        }
        if (sourceid == SuperGM.HEAL_PLUS_DISPEL)
        {
            mpchange += applyfrom.getCurrentMaxMp();
        }

        return mpchange;
    }

    private int alchemistModifyVal(IPlayer chr, int val, bool withX)
    {
        if (!skill && (chr.getJob().isA(Job.HERMIT) || chr.getJob().isA(Job.NIGHTWALKER3)))
        {
            var alchemistEffect = getAlchemistEffect(chr);
            if (alchemistEffect != null)
            {
                return (int)(val * ((withX ? alchemistEffect.getX() : alchemistEffect.getY()) / 100.0));
            }
        }
        return val;
    }

    private StatEffect? getAlchemistEffect(IPlayer chr)
    {
        int id = Hermit.ALCHEMIST;
        if (chr.isCygnus())
        {
            id = NightWalker.ALCHEMIST;
        }
        var skill = SkillFactory.GetSkillTrust(id);
        int alchemistLevel = chr.getSkillLevel(skill);
        return alchemistLevel == 0 ? null : skill.getEffect(alchemistLevel);
    }

    private bool isGmBuff()
    {
        switch (sourceid)
        {
            case Beginner.ECHO_OF_HERO:
            case Noblesse.ECHO_OF_HERO:
            case Legend.ECHO_OF_HERO:
            case Evan.ECHO_OF_HERO:
            case SuperGM.HEAL_PLUS_DISPEL:
            case SuperGM.HASTE:
            case SuperGM.HOLY_SYMBOL:
            case SuperGM.BLESS:
            case SuperGM.RESURRECTION:
            case SuperGM.HYPER_BODY:
                return true;
            default:
                return false;
        }
    }

    private bool isMonsterBuff()
    {
        if (!skill)
        {
            return false;
        }
        switch (sourceid)
        {
            case Page.THREATEN:
            case FPWizard.SLOW:
            case ILWizard.SLOW:
            case FPMage.SEAL:
            case ILMage.SEAL:
            case Priest.DOOM:
            case Hermit.SHADOW_WEB:
            case NightLord.NINJA_AMBUSH:
            case Shadower.NINJA_AMBUSH:
            case BlazeWizard.SLOW:
            case BlazeWizard.SEAL:
            case NightWalker.SHADOW_WEB:
            case Crusader.ARMOR_CRASH:
            case DragonKnight.POWER_CRASH:
            case WhiteKnight.MAGIC_CRASH:
            case Priest.DISPEL:
            case SuperGM.HEAL_PLUS_DISPEL:
                return true;
        }
        return false;
    }

    private bool isPartyBuff()
    {
        if (lt == null || rb == null)
        {
            return false;
        }
        // wk charges have lt and rb set but are neither player nor monster buffs
        return (sourceid < 1211003 || sourceid > 1211008) && sourceid != Paladin.SWORD_HOLY_CHARGE && sourceid != Paladin.BW_HOLY_CHARGE && sourceid != DawnWarrior.SOUL_CHARGE;
    }

    private bool isHeal()
    {
        return sourceid == Cleric.HEAL || sourceid == SuperGM.HEAL_PLUS_DISPEL;
    }

    private bool isResurrection()
    {
        return sourceid == Bishop.RESURRECTION || sourceid == GM.RESURRECTION || sourceid == SuperGM.RESURRECTION;
    }

    private bool isTimeLeap()
    {
        return sourceid == Buccaneer.TIME_LEAP;
    }

    public bool isDragonBlood()
    {
        return skill && sourceid == DragonKnight.DRAGON_BLOOD;
    }

    public bool isBerserk()
    {
        return skill && sourceid == DarkKnight.BERSERK;
    }

    public bool isRecovery()
    {
        return sourceid == Beginner.RECOVERY || sourceid == Noblesse.RECOVERY || sourceid == Legend.RECOVERY || sourceid == Evan.RECOVERY;
    }

    public bool isMapChair()
    {
        return sourceid == Beginner.MAP_CHAIR || sourceid == Noblesse.MAP_CHAIR || sourceid == Legend.MAP_CHAIR;
    }

    public static bool isMapChair(int sourceid)
    {
        return sourceid == Beginner.MAP_CHAIR || sourceid == Noblesse.MAP_CHAIR || sourceid == Legend.MAP_CHAIR;
    }

    public static bool isHpMpRecovery(int sourceid)
    {
        return sourceid == ItemId.RUSSELLONS_PILLS || sourceid == ItemId.SORCERERS_POTION;
    }

    public static bool isAriantShield(int sourceid)
    {
        return sourceid == ItemId.ARPQ_SHIELD;
    }

    private bool isDs()
    {
        return skill && (sourceid == Rogue.DARK_SIGHT || sourceid == NightWalker.DARK_SIGHT);
    }

    private bool isWw()
    {
        return skill && (sourceid == WindArcher.WIND_WALK);
    }

    private bool isCombo()
    {
        return skill && (sourceid == Crusader.COMBO || sourceid == DawnWarrior.COMBO);
    }

    private bool isEnrage()
    {
        return skill && sourceid == Hero.ENRAGE;
    }

    public bool isBeholder()
    {
        return skill && sourceid == DarkKnight.BEHOLDER;
    }

    private bool isShadowPartner()
    {
        return skill && (sourceid == Hermit.SHADOW_PARTNER || sourceid == NightWalker.SHADOW_PARTNER);
    }

    private bool isChakra()
    {
        return skill && sourceid == ChiefBandit.CHAKRA;
    }

    private bool isCouponBuff()
    {
        return ItemId.isRateCoupon(sourceid);
    }

    private bool isAriantShield()
    {
        int itemid = sourceid;
        return isAriantShield(itemid);
    }

    private bool isMysticDoor()
    {
        return skill && sourceid == Priest.MYSTIC_DOOR;
    }

    public bool isMonsterRiding()
    {
        return skill && (sourceid % 10000000 == 1004 || sourceid == Corsair.BATTLE_SHIP || sourceid == Beginner.SPACESHIP || sourceid == Noblesse.SPACESHIP
                || sourceid == Beginner.YETI_MOUNT1 || sourceid == Beginner.YETI_MOUNT2 || sourceid == Beginner.WITCH_BROOMSTICK || sourceid == Beginner.BALROG_MOUNT
                || sourceid == Noblesse.YETI_MOUNT1 || sourceid == Noblesse.YETI_MOUNT2 || sourceid == Noblesse.WITCH_BROOMSTICK || sourceid == Noblesse.BALROG_MOUNT
                || sourceid == Legend.YETI_MOUNT1 || sourceid == Legend.YETI_MOUNT2 || sourceid == Legend.WITCH_BROOMSTICK || sourceid == Legend.BALROG_MOUNT);
    }

    public bool isMagicDoor()
    {
        return skill && sourceid == Priest.MYSTIC_DOOR;
    }

    public bool isPoison()
    {
        return skill && (sourceid == FPMage.POISON_MIST || sourceid == FPWizard.POISON_BREATH || sourceid == FPMage.ELEMENT_COMPOSITION || sourceid == NightWalker.POISON_BOMB || sourceid == BlazeWizard.FLAME_GEAR);
    }

    public bool isMorph()
    {
        return morphId > 0;
    }

    public bool isMorphWithoutAttack()
    {
        return morphId > 0 && morphId < 100; // Every morph item I have found has been under 100, pirate skill transforms start at 1000.
    }

    private bool isMist()
    {
        return skill 
            && 
            (sourceid == FPMage.POISON_MIST 
            || sourceid == Shadower.SMOKE_SCREEN 
            || sourceid == BlazeWizard.FLAME_GEAR 
            || sourceid == NightWalker.POISON_BOMB 
            || sourceid == Evan.RECOVERY_AURA);
    }

    private bool isSoulArrow()
    {
        return skill && (sourceid == Hunter.SOUL_ARROW || sourceid == Crossbowman.SOUL_ARROW || sourceid == WindArcher.SOUL_ARROW);
    }

    private bool isShadowClaw()
    {
        return skill && sourceid == NightLord.SHADOW_STARS;
    }

    private bool isCrash()
    {
        return skill && (sourceid == DragonKnight.POWER_CRASH || sourceid == Crusader.ARMOR_CRASH || sourceid == WhiteKnight.MAGIC_CRASH);
    }

    private bool isSeal()
    {
        return skill && (sourceid == ILMage.SEAL || sourceid == FPMage.SEAL || sourceid == BlazeWizard.SEAL);
    }

    private bool isDispel()
    {
        return skill && (sourceid == Priest.DISPEL || sourceid == SuperGM.HEAL_PLUS_DISPEL);
    }

    private bool isCureAllAbnormalStatus()
    {
        if (skill)
        {
            return isHerosWill(sourceid);
        }
        else
        {
            return sourceid == ItemId.WHITE_ELIXIR;
        }
    }

    public static bool isHerosWill(int skillid)
    {
        switch (skillid)
        {
            case Hero.HEROS_WILL:
            case Paladin.HEROS_WILL:
            case DarkKnight.HEROS_WILL:
            case FPArchMage.HEROS_WILL:
            case ILArchMage.HEROS_WILL:
            case Bishop.HEROS_WILL:
            case Bowmaster.HEROS_WILL:
            case Marksman.HEROS_WILL:
            case NightLord.HEROS_WILL:
            case Shadower.HEROS_WILL:
            case Buccaneer.PIRATES_RAGE:
            case Aran.HEROS_WILL:
                return true;

            default:
                return false;
        }
    }

    private bool isWkCharge()
    {
        if (!skill)
        {
            return false;
        }

        foreach (var p in statups)
        {
            if (p.BuffState.Equals(BuffStat.WK_CHARGE))
            {
                return true;
            }
        }

        return false;
    }

    private bool isDash()
    {
        return skill && (sourceid == Pirate.DASH || sourceid == ThunderBreaker.DASH || sourceid == Beginner.SPACE_DASH || sourceid == Noblesse.SPACE_DASH);
    }

    private bool isSkillMorph()
    {
        return skill && (sourceid == Buccaneer.SUPER_TRANSFORMATION || sourceid == Marauder.TRANSFORMATION || sourceid == WindArcher.EAGLE_EYE || sourceid == ThunderBreaker.TRANSFORMATION);
    }

    private bool isInfusion()
    {
        return skill && (sourceid == Buccaneer.SPEED_INFUSION || sourceid == Corsair.SPEED_INFUSION || sourceid == ThunderBreaker.SPEED_INFUSION);
    }

    private bool isCygnusFA()
    {
        return skill && (sourceid == DawnWarrior.FINAL_ATTACK || sourceid == WindArcher.FINAL_ATTACK);
    }

    private bool isHyperBody()
    {
        return skill && (sourceid == Spearman.HYPER_BODY || sourceid == GM.HYPER_BODY || sourceid == SuperGM.HYPER_BODY);
    }

    private bool isComboReset()
    {
        return sourceid == Aran.COMBO_BARRIER || sourceid == Aran.COMBO_DRAIN;
    }

    private int getFatigue()
    {
        return fatigue;
    }

    private int getMorph()
    {
        return morphId;
    }

    private int getMorph(IPlayer chr)
    {
        if (morphId == 1000 || morphId == 1001 || morphId == 1003)
        { // morph skill
            return chr.getGender() == 0 ? morphId : morphId + 100;
        }
        return morphId;
    }

    private SummonMovementType? getSummonMovementType()
    {
        if (!skill)
        {
            return null;
        }
        switch (sourceid)
        {
            case Ranger.PUPPET:
            case Sniper.PUPPET:
            case WindArcher.PUPPET:
            case Outlaw.OCTOPUS:
            case Corsair.WRATH_OF_THE_OCTOPI:
                return SummonMovementType.STATIONARY;
            case Ranger.SILVER_HAWK:
            case Sniper.GOLDEN_EAGLE:
            case Priest.SUMMON_DRAGON:
            case Marksman.FROST_PREY:
            case Bowmaster.PHOENIX:
            case Outlaw.GAVIOTA:
                return SummonMovementType.CIRCLE_FOLLOW;
            case DarkKnight.BEHOLDER:
            case FPArchMage.ELQUINES:
            case ILArchMage.IFRIT:
            case Bishop.BAHAMUT:
            case DawnWarrior.SOUL:
            case BlazeWizard.FLAME:
            case BlazeWizard.IFRIT:
            case WindArcher.STORM:
            case NightWalker.DARKNESS:
            case ThunderBreaker.LIGHTNING:
                return SummonMovementType.FOLLOW;
        }
        return null;
    }

    public bool isSkill()
    {
        return skill;
    }

    public int getSourceId()
    {
        return sourceid;
    }

    public int getBuffSourceId()
    {
        return skill ? sourceid : -sourceid;
    }

    public bool makeChanceResult()
    {
        return prop == 1.0 || Randomizer.nextDouble() < prop;
    }

    /*
     private static class CancelEffectAction : Runnable {

     private StatEffect effect;
     private WeakReference<IPlayer> target;
     private long startTime;

     public CancelEffectAction(IPlayer target, StatEffect effect, long startTime) {
     this.effect = effect;
     this.target = new WeakReference<>(target);
     this.startTime = startTime;
     }

     public override void run() {
     IPlayer realTarget = target.get();
     if (realTarget != null) {
     realTarget.cancelEffect(effect, false, startTime);
     }
     }
     }
     */
    public short getHp()
    {
        return hp;
    }

    public short getMp()
    {
        return mp;
    }

    public double getHpRate()
    {
        return hpR;
    }

    public double getMpRate()
    {
        return mpR;
    }

    public byte getHpR()
    {
        return mhpR;
    }

    public byte getMpR()
    {
        return mmpR;
    }

    public short getHpRRate()
    {
        return mhpRRate;
    }

    public short getMpRRate()
    {
        return mmpRRate;
    }

    public short getHpCon()
    {
        return hpCon;
    }

    public short getMpCon()
    {
        return mpCon;
    }

    public short getMatk()
    {
        return matk;
    }

    public short getWatk()
    {
        return watk;
    }

    public int getDuration()
    {
        return duration;
    }

    public List<BuffStatValue> getStatups()
    {
        return statups;
    }

    public bool sameSource(StatEffect effect)
    {
        return this.sourceid == effect.sourceid && this.skill == effect.skill;
    }

    public int getX()
    {
        return x;
    }

    public int getY()
    {
        return y;
    }

    public int getDamage()
    {
        return damage;
    }

    public int getAttackCount()
    {
        return attackCount;
    }

    public int getMobCount()
    {
        return mobCount;
    }

    public int getFixDamage()
    {
        return fixdamage;
    }

    public short getBulletCount()
    {
        return bulletCount;
    }

    public short getBulletConsume()
    {
        return bulletConsume;
    }

    public int getMoneyCon()
    {
        return moneyCon;
    }

    public int getCooldown()
    {
        return cooldown;
    }

    public Dictionary<MonsterStatus, int> getMonsterStati()
    {
        return monsterStatus;
    }
}
