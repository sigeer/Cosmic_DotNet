namespace server.gachapon;

/**
 * @author Alan (SharpAceX) - gachapon source classes stub & pirate items
 * @author Ronan - parsed MapleSEA loots
 * <p>
 * MapleSEA-like loots thanks to AyumiLove - src: https://ayumilovemaple.wordpress.com/maplestory-gachapon-guide/
 */

public class NautilusHarbor : GachaponItems
{

    public override int[] getCommonItems()
    {
        return new int[]{
                /* Scroll */
                2040605, 2040626, 2040609, 2040607, 2041029, 2041027, 2041031, 2041037, 2041033, 2041039, 2041041, 2041035,
                2040809, 2040813, 2040015, 2040009, 2040011, 2040013, 2040509, 2040521, 2040519, 2040507, 2040905, 2040909,
                2040907, 2040713, 2040715, 2040717, 2040405, 2040409, 2040407, 2040426, 2040303, 2040307, 2040309, 2044505,
                2044705, 2044605, 2043305, 2043105, 2043205, 2043005, 2043007, 2044405, 2044305, 2043805, 2044105, 2044205,
                2044005, 2043705, 2044901,

                /* Useable drop */
                2012000, 2000004, 2020008, 2000005, 2012002, 2101004, 2101005, 2101002, 2101003, 4006000,

                /* Warrior equipment */
                1092014, 1402017,

                /* Magician equipment */
                1002037, 1002034, 1002064, 1002038, 1382037, 1372000, 1002013, 1002035, 1002065, 1382000,

                /* Bowman equipment */
                1452018,

                /* Thief equipment */
                1472010, 1002175, 1472017, 1472025,

                /* Pirate equipment */
                1002610, 1002616, 1002622, 1002628, 1002634, 1002640, 1002646, 1052095, 1052101, 1052107,
                1052113, 1052119, 1052125, 1052131, 1072285, 1072291, 1072297, 1072303, 1072309, 1072315,
                1082180, 1082186, 1082192, 1082198, 1082204, 1082210, 1482001, 1482003, 1482005, 1482007,
                1482009, 1482011, 1492000, 1492002, 1492004, 1492006, 1492008, 1492010, 1492012, 1002613,
                1002619, 1002625, 1002631, 1002637, 1002643, 1052098, 1052104, 1052110, 1052116, 1052122,
                1052128, 1072288, 1072294, 1072300, 1072306, 1072312, 1072318, 1072338, 1082183, 1082189,
                1082195, 1082201, 1082207, 1082213, 1482000, 1482002, 1482004, 1482006, 1482008, 1482010,
                1482012, 1492001, 1492003, 1492005, 1492007, 1492009, 1492011,

                /* Knuckler Scrolls */
                2044800, 2044801, 2044802, 2044803, 2044804, 2044805, 2044806, 2044807, 2044808, 2044809,

                /* Gun Scrolls */
                2044900, 2044901, 2044902, 2044903, 2044904
        };
    }

    public override int[] getUncommonItems()
    {
        return new int[] { 2040811, 2040815, 2101001 };
    }

    public override int[] getRareItems()
    {
        return new int[] { };
    }

}
