using Application.Shared;

namespace Application.Core.Managers
{
    public static class ResManager
    {
        private static Data npcStringData;
        private static Data mobStringData;
        private static Data skillStringData;
        private static Data mapStringData;

        static ResManager()
        {

            DataProvider dataProvider = DataProviderFactory.getDataProvider(WZFiles.STRING);
            npcStringData = dataProvider.getData("Npc.img");
            mobStringData = dataProvider.getData("Mob.img");
            skillStringData = dataProvider.getData("Skill.img");
            mapStringData = dataProvider.getData("Map.img");
        }

        public static WzFindResult<WzFindResultItem> FindMobIdByName(string name)
        {
            var list = new List<WzFindResultItem>();
            foreach (Data searchData in mobStringData.getChildren())
            {
                var itemName = DataTool.getString(searchData.getChildByPath("name")) ?? "NO-NAME";
                if (itemName.Contains(name, StringComparison.OrdinalIgnoreCase) && int.TryParse(searchData.getName(), out var id))
                {
                    list.Add(new WzFindResultItem(id, itemName));
                }
            }
            return new WzFindResult<WzFindResultItem>(list, name);
        }

        public static WzFindResult<WzFindMapResultItem> FindMapIdByName(string name)
        {
            var list = new List<WzFindMapResultItem>();

            string mapName, streetName;

            foreach (Data searchDataDir in mapStringData.getChildren())
            {
                foreach (Data searchData in searchDataDir.getChildren())
                {
                    mapName = DataTool.getString(searchData.getChildByPath("mapName")) ?? "NO-NAME";
                    streetName = DataTool.getString(searchData.getChildByPath("streetName")) ?? "NO-NAME";

                    if (int.TryParse(searchData.getName(), out var id) 
                        && (mapName.Contains(name, StringComparison.OrdinalIgnoreCase) || streetName.Contains(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        list.Add(new WzFindMapResultItem(id, mapName, streetName));
                    }
                }
            }

            return new WzFindResult<WzFindMapResultItem>(list, name);
        }
    }
}
