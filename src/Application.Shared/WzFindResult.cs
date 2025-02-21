using System.Linq;

namespace Application.Shared
{
    public class WzFindResult<T> where T : WzFindResultItem
    {
        public WzFindResult(List<T> matchedItems, string searchText)
        {
            MatchedItems = matchedItems;

            var fullMatchedItems = MatchedItems.Where(x => x.Name.Equals(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
            if (fullMatchedItems.Count == 1)
                BestMatch = fullMatchedItems[0];
        }

        public List<T> MatchedItems { get; set; }
        public T? BestMatch { get; set; }
    }

    public class WzFindResultItem
    {
        public WzFindResultItem(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class WzFindMapResultItem: WzFindResultItem
    {
        public WzFindMapResultItem(int id, string name, string streetName) : base(id, name)
        {
            StreetName = streetName;
        }

        public string StreetName { get; set; }
    }
}
