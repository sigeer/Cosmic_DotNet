

using Application.Core.Game.Maps;
using client;
using constants.id;
using constants.skills;
using provider;
using provider.wz;
using server.maps;
using server.quest;
using System.Globalization;
using System.Text.RegularExpressions;

namespace constants.game;


/*
 * @author kevintjuh93
 * @author Ronan
 */
public class GameConstants
{
    public static string[] WORLD_NAMES = { "Scania", "Bera", "Broa", "Windia", "Khaini", "Bellocan", "Mardia", "Kradia", "Yellonde", "Demethos", "Galicia", "El Nido", "Zenith", "Arcenia", "Kastia", "Judis", "Plana", "Kalluna", "Stius", "Croa", "Medere" };
    public static string[] stats = { "tuc", "reqLevel", "reqJob", "reqSTR", "reqDEX", "reqINT", "reqLUK", "reqPOP", "cash", "cursed", "success", "setItemID", "equipTradeBlock", "durability", "randOption", "randStat", "masterLevel", "reqSkillLevel", "elemDefault", "incRMAS", "incRMAF", "incRMAI", "incRMAL", "canLevel", "skill", "charmEXP" };
    public static int[] CASH_DATA = new int[] { 50200004, 50200069, 50200117, 50100008, 50000047 };

    // Ronan's rates upgrade system
    private static int[] DROP_RATE_GAIN = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
    private static int[] MESO_RATE_GAIN = { 1, 3, 6, 10, 15, 21, 28, 36, 45, 55, 66, 78, 91, 105 };
    private static int[] EXP_RATE_GAIN = { 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610 };    //fibonacci :3

    private static int[] jobUpgradeBlob = { 1, 20, 60, 110, 190 };
    private static int[] jobUpgradeSpUp = { 0, 1, 2, 3, 6 };
    private static Dictionary<int, string> jobNames = new();

    static CultureInfo FR = new CultureInfo("fr-FR");
    static CultureInfo EN = new CultureInfo("en-GB");
    static CultureInfo Culture = YamlConfig.config.server.USE_UNITPRICE_WITH_COMMA ? FR : EN;

    public static Disease[] CPQ_DISEASES = {Disease.SLOW, Disease.SEDUCE, Disease.STUN, Disease.POISON,
            Disease.SEAL, Disease.DARKNESS, Disease.WEAKEN, Disease.CURSE};

    public static int MAX_FIELD_MOB_DAMAGE = getMaxObstacleMobDamageFromWz() * 2;

    public static int getPlayerBonusDropRate(int slot)
    {
        return (DROP_RATE_GAIN[slot]);
    }

    public static int getPlayerBonusMesoRate(int slot)
    {
        return (MESO_RATE_GAIN[slot]);
    }

    public static int getPlayerBonusExpRate(int slot)
    {
        return (EXP_RATE_GAIN[slot]);
    }

    // "goto" command for players
    public static Dictionary<string, int> GOTO_TOWNS = new Dictionary<string, int>()
    {
        {"southperry", 60000 },
        {"amherst", 1000000},
        {"henesys", 100000000},
        {"ellinia", 101000000},
        {"perion", 102000000},
        {"kerning", 103000000},
        {"lith", 104000000},
        {"sleepywood", 105040300},
        {"florina", 110000000},
        {"nautilus", 120000000},
        {"ereve", 130000000},
        {"rien", 140000000},
        {"orbis", 200000000},
        {"happy", 209000000},
        {"elnath", 211000000},
        {"ludi", 220000000},
        {"aqua", 230000000},
        {"leafre", 240000000},
        {"mulung", 250000000},
        {"herb", 251000000},
        {"omega", 221000000},
        {"korean", 222000000},
        {"ellin", 300000000},
        {"nlc", 600000000},
        {"showa", 801000000},
        {"shrine", 800000000},
        {"ariant", 260000000},
        {"magatia", 261000000},
        {"singapore", 540000000},
        {"quay", 541000000},
        {"kampung", 551000000},
        {"amoria", 680000000},
        {"temple", 270000100},
        {"square", 103040000},
        {"neo", 240070000},
        {"mushking", 106020000}
    };

    // "goto" command for only-GMs
    public static Dictionary<string, int> GOTO_AREAS = new Dictionary<string, int>() {
        {"gmmap", 180000000},
{"excavation", 990000000},
{"mushmom", 100000005},
{"griffey", 240020101},
{"manon", 240020401},
{"horseman", 682000001},
{"balrog", 105090900},
{"zakum", 211042300},
{"papu", 220080001},
{"guild", 200000301},
{"skelegon", 240040511},
{"hpq", 100000200},
{"pianus", 230040420},
{"horntail", 240050400},
{"pinkbean", 270050000},
{"keep", 610020006},
{"dojo", 925020001},
{"bosspq", 970030000},
{"fm", 910000000},
 };

    public static List<string> GAME_SONGS = new List<string>(170) {
        "Jukebox/Congratulation",
"Bgm00/SleepyWood",
"Bgm00/FloralLife",
"Bgm00/GoPicnic",
"Bgm00/Nightmare",
"Bgm00/RestNPeace",
"Bgm01/AncientMove",
"Bgm01/MoonlightShadow",
"Bgm01/WhereTheBarlogFrom",
"Bgm01/CavaBien",
"Bgm01/HighlandStar",
"Bgm01/BadGuys",
"Bgm02/MissingYou",
"Bgm02/WhenTheMorningComes",
"Bgm02/EvilEyes",
"Bgm02/JungleBook",
"Bgm02/AboveTheTreetops",
"Bgm03/Subway",
"Bgm03/Elfwood",
"Bgm03/BlueSky",
"Bgm03/Beachway",
"Bgm03/SnowyVillage",
"Bgm04/PlayWithMe",
"Bgm04/WhiteChristmas",
"Bgm04/UponTheSky",
"Bgm04/ArabPirate",
"Bgm04/Shinin'Harbor",
"Bgm04/WarmRegard",
"Bgm05/WolfWood",
"Bgm05/DownToTheCave",
"Bgm05/AbandonedMine",
"Bgm05/MineQuest",
"Bgm05/HellGate",
"Bgm06/FinalFight",
"Bgm06/WelcomeToTheHell",
"Bgm06/ComeWithMe",
"Bgm06/FlyingInABlueDream",
"Bgm06/FantasticThinking",
"Bgm07/WaltzForWork",
"Bgm07/WhereverYouAre",
"Bgm07/FunnyTimeMaker",
"Bgm07/HighEnough",
"Bgm07/Fantasia",
"Bgm08/LetsMarch",
"Bgm08/ForTheGlory",
"Bgm08/FindingForest",
"Bgm08/LetsHuntAliens",
"Bgm08/PlotOfPixie",
"Bgm09/DarkShadow",
"Bgm09/TheyMenacingYou",
"Bgm09/FairyTale",
"Bgm09/FairyTalediffvers",
"Bgm09/TimeAttack",
"Bgm10/Timeless",
"Bgm10/TimelessB",
"Bgm10/BizarreTales",
"Bgm10/TheWayGrotesque",
"Bgm10/Eregos",
"Bgm11/BlueWorld",
"Bgm11/Aquarium",
"Bgm11/ShiningSea",
"Bgm11/DownTown",
"Bgm11/DarkMountain",
"Bgm12/AquaCave",
"Bgm12/DeepSee",
"Bgm12/WaterWay",
"Bgm12/AcientRemain",
"Bgm12/RuinCastle",
"Bgm12/Dispute",
"Bgm13/CokeTown",
"Bgm13/Leafre",
"Bgm13/Minar'sDream",
"Bgm13/AcientForest",
"Bgm13/TowerOfGoddess",
"Bgm14/DragonLoad",
"Bgm14/HonTale",
"Bgm14/CaveOfHontale",
"Bgm14/DragonNest",
"Bgm14/Ariant",
"Bgm14/HotDesert",
"Bgm15/MureungHill",
"Bgm15/MureungForest",
"Bgm15/WhiteHerb",
"Bgm15/Pirate",
"Bgm15/SunsetDesert",
"Bgm16/Duskofgod",
"Bgm16/FightingPinkBeen",
"Bgm16/Forgetfulness",
"Bgm16/Remembrance",
"Bgm16/Repentance",
"Bgm16/TimeTemple",
"Bgm17/MureungSchool1",
"Bgm17/MureungSchool2",
"Bgm17/MureungSchool3",
"Bgm17/MureungSchool4",
"Bgm18/BlackWing",
"Bgm18/DrillHall",
"Bgm18/QueensGarden",
"Bgm18/RaindropFlower",
"Bgm18/WolfAndSheep",
"Bgm19/BambooGym",
"Bgm19/CrystalCave",
"Bgm19/MushCatle",
"Bgm19/RienVillage",
"Bgm19/SnowDrop",
"Bgm20/GhostShip",
"Bgm20/NetsPiramid",
"Bgm20/UnderSubway",
"Bgm21/2021year",
"Bgm21/2099year",
"Bgm21/2215year",
"Bgm21/2230year",
"Bgm21/2503year",
"Bgm21/KerningSquare",
"Bgm21/KerningSquareField",
"Bgm21/KerningSquareSubway",
"Bgm21/TeraForest",
"BgmEvent/FunnyRabbit",
"BgmEvent/FunnyRabbitFaster",
"BgmEvent/wedding",
"BgmEvent/weddingDance",
"BgmEvent/wichTower",
"BgmGL/amoria",
"BgmGL/Amorianchallenge",
"BgmGL/chapel",
"BgmGL/cathedral",
"BgmGL/Courtyard",
"BgmGL/CrimsonwoodKeep",
"BgmGL/CrimsonwoodKeepInterior",
"BgmGL/GrandmastersGauntlet",
"BgmGL/HauntedHouse",
"BgmGL/NLChunt",
"BgmGL/NLCtown",
"BgmGL/NLCupbeat",
"BgmGL/PartyQuestGL",
"BgmGL/PhantomForest",
"BgmJp/Feeling",
"BgmJp/BizarreForest",
"BgmJp/Hana",
"BgmJp/Yume",
"BgmJp/Bathroom",
"BgmJp/BattleField",
"BgmJp/FirstStepMaster",
"BgmMY/Highland",
"BgmMY/KualaLumpur",
"BgmSG/BoatQuay_field",
"BgmSG/BoatQuay_town",
"BgmSG/CBD_field",
"BgmSG/CBD_town",
"BgmSG/Ghostship",
"BgmUI/ShopBgm",
"BgmUI/Title",
   };

    // MapleStory default keyset
    private static int[] DEFAULT_KEY = { 18, 65, 2, 23, 3, 4, 5, 6, 16, 17, 19, 25, 26, 27, 31, 34, 35, 37, 38, 40, 43, 44, 45, 46, 50, 56, 59, 60, 61, 62, 63, 64, 57, 48, 29, 7, 24, 33, 41, 39 };
    private static int[] DEFAULT_TYPE = { 4, 6, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 4, 4, 5, 6, 6, 6, 6, 6, 6, 5, 4, 5, 4, 4, 4, 4, 4 };
    private static int[] DEFAULT_ACTION = { 0, 106, 10, 1, 12, 13, 18, 24, 8, 5, 4, 19, 14, 15, 2, 17, 11, 3, 20, 16, 9, 50, 51, 6, 7, 53, 100, 101, 102, 103, 104, 105, 54, 22, 52, 21, 25, 26, 23, 27 };

    // HeavenMS custom keyset
    private static int[] CUSTOM_KEY = { 2, 3, 4, 5, 31, 56, 59, 32, 42, 6, 17, 29, 30, 41, 50, 60, 61, 62, 63, 64, 65, 16, 7, 9, 13, 8 };
    private static int[] CUSTOM_TYPE = { 4, 4, 4, 4, 5, 5, 6, 5, 5, 4, 4, 4, 5, 4, 4, 6, 6, 6, 6, 6, 6, 4, 4, 4, 4, 4 };
    private static int[] CUSTOM_ACTION = { 1, 0, 3, 2, 53, 54, 100, 52, 51, 19, 5, 9, 50, 7, 22, 101, 102, 103, 104, 105, 106, 8, 17, 26, 20, 4 };

    public static int[] getCustomKey(bool customKeyset)
    {
        return (customKeyset ? CUSTOM_KEY : DEFAULT_KEY);
    }

    public static int[] getCustomType(bool customKeyset)
    {
        return (customKeyset ? CUSTOM_TYPE : DEFAULT_TYPE);
    }

    public static int[] getCustomAction(bool customKeyset)
    {
        return (customKeyset ? CUSTOM_ACTION : DEFAULT_ACTION);
    }

    private static int[] mobHpVal = {0, 15, 20, 25, 35, 50, 65, 80, 95, 110, 125, 150, 175, 200, 225, 250, 275, 300, 325, 350,
            375, 405, 435, 465, 495, 525, 580, 650, 720, 790, 900, 990, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800,
            1900, 2000, 2100, 2200, 2300, 2400, 2520, 2640, 2760, 2880, 3000, 3200, 3400, 3600, 3800, 4000, 4300, 4600, 4900, 5200,
            5500, 5900, 6300, 6700, 7100, 7500, 8000, 8500, 9000, 9500, 10000, 11000, 12000, 13000, 14000, 15000, 17000, 19000, 21000, 23000,
            25000, 27000, 29000, 31000, 33000, 35000, 37000, 39000, 41000, 43000, 45000, 47000, 49000, 51000, 53000, 55000, 57000, 59000, 61000, 63000,
            65000, 67000, 69000, 71000, 73000, 75000, 77000, 79000, 81000, 83000, 85000, 89000, 91000, 93000, 95000, 97000, 99000, 101000, 103000,
            105000, 107000, 109000, 111000, 113000, 115000, 118000, 120000, 125000, 130000, 135000, 140000, 145000, 150000, 155000, 160000, 165000, 170000, 175000, 180000,
            185000, 190000, 195000, 200000, 205000, 210000, 215000, 220000, 225000, 230000, 235000, 240000, 250000, 260000, 270000, 280000, 290000, 300000, 310000, 320000,
            330000, 340000, 350000, 360000, 370000, 380000, 390000, 400000, 410000, 420000, 430000, 440000, 450000, 460000, 470000, 480000, 490000, 500000, 510000, 520000,
            530000, 550000, 570000, 590000, 610000, 630000, 650000, 670000, 690000, 710000, 730000, 750000, 770000, 790000, 810000, 830000, 850000, 870000, 890000, 910000};

    public static string getJobName(int jobid)
    {
        string? name = jobNames.GetValueOrDefault(jobid);

        if (name == null)
        {
            Job job = JobUtils.getById(jobid);

            name = job.ToString().ToLower();
            name = Regex.Replace(name, "[0-9]", "");
            name = name.Substring(0, 1).ToUpper() + name.Substring(1);

            jobNames.AddOrUpdate(jobid, name);
        }

        return name;
    }

    public static int getJobUpgradeLevelRange(int jobbranch)
    {
        return jobUpgradeBlob[jobbranch];
    }

    public static int getChangeJobSpUpgrade(int jobbranch)
    {
        return jobUpgradeSpUp[jobbranch];
    }

    public static bool isHallOfFameMap(int mapid)
    {
        switch (mapid)
        {
            case MapId.HALL_OF_WARRIORS:     // warrior
            case MapId.HALL_OF_MAGICIANS:     // magician
            case MapId.HALL_OF_BOWMEN:     // bowman
            case MapId.HALL_OF_THIEVES:     // thief
            case MapId.NAUTILUS_TRAINING_ROOM:     // pirate
            case MapId.KNIGHTS_CHAMBER:     // cygnus
            case MapId.KNIGHTS_CHAMBER_LARGE:     // other cygnus
            case MapId.KNIGHTS_CHAMBER_2:     // cygnus 2nd floor
            case MapId.KNIGHTS_CHAMBER_3:     // cygnus 3rd floor (beginners)
            case MapId.PALACE_OF_THE_MASTER:     // aran
                return true;

            default:
                return false;
        }
    }

    public static bool isPodiumHallOfFameMap(int mapid)
    {
        switch (mapid)
        {
            case MapId.HALL_OF_WARRIORS:
            case MapId.HALL_OF_MAGICIANS:     // magician
            case MapId.HALL_OF_BOWMEN:     // bowman
            case MapId.HALL_OF_THIEVES:     // thief
            case MapId.NAUTILUS_TRAINING_ROOM:     // pirate
                return true;

            default:
                return false;
        }
    }

    public static byte getHallOfFameBranch(Job job, int mapid)
    {
        if (!isHallOfFameMap(mapid))
        {
            return (byte)(26 + 4 * (mapid / 100000000));   // custom, 400 pnpcs available per continent
        }

        if (job.isA(Job.WARRIOR))
        {
            return 10;
        }
        else if (job.isA(Job.MAGICIAN))
        {
            return 11;
        }
        else if (job.isA(Job.BOWMAN))
        {
            return 12;
        }
        else if (job.isA(Job.THIEF))
        {
            return 13;
        }
        else if (job.isA(Job.PIRATE))
        {
            return 14;
        }
        else if (job.isA(Job.DAWNWARRIOR1))
        {
            return 15;
        }
        else if (job.isA(Job.BLAZEWIZARD1))
        {
            return 16;
        }
        else if (job.isA(Job.WINDARCHER1))
        {
            return 17;
        }
        else if (job.isA(Job.NIGHTWALKER1))
        {
            return 18;
        }
        else if (job.isA(Job.THUNDERBREAKER1))
        {
            return 19;
        }
        else if (job.isA(Job.ARAN1))
        {
            return 20;
        }
        else if (job.isA(Job.EVAN1))
        {
            return 21;
        }
        else if (job.isA(Job.BEGINNER))
        {
            return 22;
        }
        else if (job.isA(Job.NOBLESSE))
        {
            return 23;
        }
        else if (job.isA(Job.LEGEND))
        {
            return 24;
        }
        else
        {
            return 25;
        }
    }

    public static int getOverallJobRankByScriptId(int scriptId)
    {
        int branch = (scriptId / 100) % 100;

        if (branch < 26)
        {
            return (scriptId % 100) + 1;
        }
        else
        {
            return ((scriptId - 2600) % 400) + 1;
        }
    }

    public static bool canPnpcBranchUseScriptId(byte branch, int scriptId)
    {
        scriptId /= 100;
        scriptId %= 100;

        if (branch < 26)
        {
            return branch == scriptId;
        }
        else
        {
            return scriptId >= branch && scriptId < branch + 4;
        }
    }

    public static int getHallOfFameMapid(Job job)
    {
        int jobid = job.getId();

        if (isCygnus(jobid))
        {
            return MapId.KNIGHTS_CHAMBER;
        }
        else if (isAran(jobid))
        {
            return MapId.PALACE_OF_THE_MASTER;
        }
        else
        {
            if (job.isA(Job.WARRIOR))
            {
                return MapId.HALL_OF_WARRIORS;
            }
            else if (job.isA(Job.MAGICIAN))
            {
                return MapId.HALL_OF_MAGICIANS;
            }
            else if (job.isA(Job.BOWMAN))
            {
                return MapId.HALL_OF_BOWMEN;
            }
            else if (job.isA(Job.THIEF))
            {
                return MapId.HALL_OF_THIEVES;
            }
            else if (job.isA(Job.PIRATE))
            {
                return MapId.NAUTILUS_TRAINING_ROOM;
            }
            else
            {
                return MapId.KNIGHTS_CHAMBER_2;   // beginner explorers are allotted with the Cygnus, available map lul
            }
        }
    }

    public static int getJobBranch(Job job)
    {
        int jobid = job.getId();

        if (jobid % 1000 == 0)
        {
            return 0;
        }
        else if (jobid % 100 == 0)
        {
            return 1;
        }
        else
        {
            return 2 + (jobid % 10);
        }
    }

    public static int getJobMaxLevel(Job job)
    {
        int jobBranch = getJobBranch(job);

        switch (jobBranch)
        {
            case 0:
                return 10;   // beginner

            case 1:
                return 30;   // 1st job

            case 2:
                return 70;   // 2nd job

            case 3:
                return 120;   // 3rd job

            default:
                return (job.getId() / 1000 == 1) ? 120 : 200;   // 4th job: cygnus is 120, rest is 200
        }
    }

    public static int getSkillBook(int job)
    {
        if (job >= 2210 && job <= 2218)
        {
            return job - 2209;
        }
        return 0;
    }

    public static bool isAranSkills(int skill)
    {
        return Aran.FULL_SWING == skill || Aran.OVER_SWING == skill || Aran.COMBO_TEMPEST == skill || Aran.COMBO_FENRIR == skill || Aran.COMBO_DRAIN == skill
                || Aran.HIDDEN_FULL_DOUBLE == skill || Aran.HIDDEN_FULL_TRIPLE == skill || Aran.HIDDEN_OVER_DOUBLE == skill || Aran.HIDDEN_OVER_TRIPLE == skill
                || Aran.COMBO_SMASH == skill || Aran.DOUBLE_SWING == skill || Aran.TRIPLE_SWING == skill;
    }

    public static bool isHiddenSkills(int skill)
    {
        return Aran.HIDDEN_FULL_DOUBLE == skill || Aran.HIDDEN_FULL_TRIPLE == skill || Aran.HIDDEN_OVER_DOUBLE == skill || Aran.HIDDEN_OVER_TRIPLE == skill;
    }

    public static bool isCygnus(int job)
    {
        return job / 1000 == 1;
    }

    public static bool isAran(int job)
    {
        return job == 2000 || (job >= 2100 && job <= 2112);
    }

    private static bool isInBranchJobTree(int skillJobId, int jobId, int branchType)
    {
        int branch = (int)(Math.Pow(10, branchType));

        int skillBranch = (skillJobId / branch) * branch;
        int jobBranch = (jobId / branch) * branch;

        return skillBranch == jobBranch;
    }

    private static bool hasDivergedBranchJobTree(int skillJobId, int jobId, int branchType)
    {
        int branch = (int)(Math.Pow(10, branchType));

        int skillBranch = skillJobId / branch;
        int jobBranch = jobId / branch;

        return skillBranch != jobBranch && skillBranch % 10 != 0;
    }

    public static bool isInJobTree(int skillId, int jobId)
    {
        int skillJob = skillId / 10000;

        if (!isInBranchJobTree(skillJob, jobId, 0))
        {
            for (int i = 1; i <= 3; i++)
            {
                if (hasDivergedBranchJobTree(skillJob, jobId, i))
                {
                    return false;
                }
                if (isInBranchJobTree(skillJob, jobId, i))
                {
                    return (skillJob <= jobId);
                }
            }
        }
        else
        {
            return (skillJob <= jobId);
        }

        return false;
    }

    public static bool isPqSkill(int skill)
    {
        return (skill >= 20000014 && skill <= 20000018) || skill == 10000013 || skill == 20001013 || (skill % 10000000 >= 1009 && skill % 10000000 <= 1011) || skill % 10000000 == 1020;
    }

    public static bool bannedBindSkills(int skill)
    {
        return isAranSkills(skill) || isPqSkill(skill);
    }

    public static bool isGMSkills(int skill)
    {
        return skill >= 9001000 && skill <= 9101008 || skill >= 8001000 && skill <= 8001001;
    }

    public static bool isFreeMarketRoom(int mapid)
    {
        return mapid / 1000000 == 910 && mapid > MapId.FM_ENTRANCE; // FM rooms subset, thanks to shavitush (shavit)
    }

    public static bool isMerchantLocked(IMap map)
    {
        if (FieldLimit.CANNOTMIGRATE.check(map.getFieldLimit()))
        {   // maps that cannot access cash shop cannot access merchants too (except FM rooms).
            return true;
        }

        return map.getId() == MapId.FM_ENTRANCE;
    }

    public static bool isDojoBossArea(int mapid)
    {
        return MapId.isDojo(mapid) && (((mapid / 100) % 100) % 6) > 0;
    }

    public static bool isAriantColiseumLobby(int mapid)
    {
        int mapbranch = mapid / 1000;
        return mapbranch == 980010 && mapid % 10 == 0;
    }

    public static bool isAriantColiseumArena(int mapid)
    {
        int mapbranch = mapid / 1000;
        return mapbranch == 980010 && mapid % 10 == 1;
    }

    public static bool isPqSkillMap(int mapid)
    {
        return MapId.isDojo(mapid) || MapId.isNettsPyramid(mapid);
    }

    public static bool isFinisherSkill(int skillId)
    {
        return skillId > 1111002 && skillId < 1111007 || skillId == 11111002 || skillId == 11111003;
    }

    public static bool isMedalQuest(short questid)
    {
        return Quest.getInstance(questid).getMedalRequirement() != -1;
    }

    public static bool hasSPTable(Job job)
    {
        switch (job)
        {
            case Job.EVAN:
            case Job.EVAN1:
            case Job.EVAN2:
            case Job.EVAN3:
            case Job.EVAN4:
            case Job.EVAN5:
            case Job.EVAN6:
            case Job.EVAN7:
            case Job.EVAN8:
            case Job.EVAN9:
            case Job.EVAN10:
                return true;
            default:
                return false;
        }
    }

    public static int getMonsterHP(int level)
    {
        if (level < 0 || level >= mobHpVal.Length)
        {
            return int.MaxValue;
        }
        return mobHpVal[level];
    }

    public static string ordinal(int i)
    {
        string[] sufixes = new string[] { "th", "st", "nd", "rd", "th", "th", "th", "th", "th", "th" };
        switch (i % 100)
        {
            case 11:
            case 12:
            case 13:
                return i + "th";

            default:
                return i + sufixes[i % 10];
        }
    }
    private static readonly NumberFormatInfo nfFormatter = new NumberFormatInfo
    {
        NumberGroupSizes = new[] { 3 },
        NumberGroupSeparator = ",",
        NumberDecimalDigits = 0
    };
    public static string numberWithCommas(int i)
    {
        if (!YamlConfig.config.server.USE_DISPLAY_NUMBERS_WITH_COMMA)
        {
            return i.ToString("N", nfFormatter);   // will display number on whatever locale is currently assigned on NumberFormat
        }
        else
        {
            return i.ToString("N", EN.NumberFormat);
        }
    }

    public static decimal parseNumber(string value)
    {
        try
        {
            return decimal.Parse(value, Culture.NumberFormat);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e.ToString());
            return 0;
        }
    }

    private static int getMaxObstacleMobDamageFromWz()
    {
        DataProvider mapSource = DataProviderFactory.getDataProvider(WZFiles.MAP);
        int maxMobDmg = 0;

        DataDirectoryEntry root = mapSource.getRoot();
        foreach (DataDirectoryEntry objData in root.getSubdirectories())
        {
            if (objData.getName() != ("Obj"))
            {
                continue;
            }

            foreach (DataFileEntry obj in objData.getFiles())
            {
                foreach (Data l0 in mapSource.getData(objData.getName() + "/" + obj.getName()).getChildren())
                {
                    foreach (Data l1 in l0.getChildren())
                    {
                        foreach (Data l2 in l1.getChildren())
                        {
                            int objDmg = DataTool.getIntConvert("s1/mobdamage", l2, 0);
                            if (maxMobDmg < objDmg)
                            {
                                maxMobDmg = objDmg;
                            }
                        }
                    }
                }
            }
        }

        return maxMobDmg;
    }

    public static string LEVEL_200 = "[Congrats] {0} has reached Level {1}! Congratulate {2} on such an amazing achievement!";
}
