using Application.Core.Game.TheWorld;
using server.maps;

namespace Application.Core.Game.Relation
{
    /// <summary>
    /// 队伍
    /// </summary>
    public interface ITeam
    {
        public int World { get; }
        public IWorld WorldServer { get; }
        void addDoor(int owner, Door door);
        void addMember(IPlayer member);
        void AssignNewLeader();
        bool containsMembers(IPlayer member);
        bool Equals(object? obj);
        Dictionary<int, Door> getDoors();
        ICollection<IPlayer> getEligibleMembers();

        int GetHashCode();
        int getId();
        IPlayer getLeader();
        int getLeaderId();
        IPlayer? getMemberById(int id);
        IPlayer GetRandomMember();
        ICollection<IPlayer> getMembers();
        List<int> getMembersSortedByHistory();
        sbyte getPartyDoor(int cid);
        List<IPlayer> getPartyMembersOnline();
        void removeDoor(int owner);
        void removeMember(IPlayer member);
        void setEligibleMembers(List<IPlayer> eliParty);
        ITeam? getEnemy();
        void setEnemy(ITeam? enemy);
        void setId(int id);
        void setLeader(IPlayer victim);
        void updateMember(IPlayer member);
    }
}
