using Application.Core.Game.Relation;
using Application.Core.Managers;

namespace Application.Core.Game.TheWorld
{
    public class WorldGuildStorage : DataStorage<IGuild>
    {
        public IGuild? GetOrAdd(int guildId)
        {
            if (_dataSource.TryGetValue(guildId, out var value) && value!.IsValid)
                return value;

            var d = GuildManager.FindGuildFromDB(guildId);
            if (d == null)
                return null;

            _dataSource.Add(guildId, d);
            return d;
        }
    }
}
