using Application.Core.Game.TheWorld;
using net.server;
using net.server.world;
using server.maps;
using System.Collections.Concurrent;
using tools;

namespace Application.Core.Game.Relation
{
    public class Team : ITeam
    {
        private int id;
        private ITeam? enemy = null;
        private int leaderId;
        private ConcurrentDictionary<int, IPlayer> members = new();
        private List<IPlayer> pqMembers = new List<IPlayer>();

        /// <summary>
        /// 队伍中所有历史成员？
        /// </summary>
        private Dictionary<int, int> histMembers = new();
        private int nextEntry = 0;

        private ConcurrentDictionary<int, Door> doors = new();

        private object lockObj = new object();
        public int World { get; }
        public IWorld WorldServer => Server.getInstance().getWorld(World);
        public Team(int id, IPlayer chrfor)
        {
            this.leaderId = chrfor.getId();
            World = chrfor.getWorld();
            this.id = id;
        }

        public bool containsMembers(IPlayer member)
        {
            return members.ContainsKey(member.Id);
        }

        public void addMember(IPlayer member)
        {
            Monitor.Enter(lockObj);
            try
            {
                histMembers.AddOrUpdate(member.getId(), nextEntry);
                nextEntry++;

                members.AddOrUpdate(member.Id, member);
            }
            finally
            {
                Monitor.Exit(lockObj);
            }
        }

        public void removeMember(IPlayer member)
        {
            Monitor.Enter(lockObj);
            try
            {
                histMembers.Remove(member.getId());

                members.Remove(member.Id);
            }
            finally
            {
                Monitor.Exit(lockObj);
            }
        }

        public void setLeader(IPlayer victim)
        {
            this.leaderId = victim.getId();
        }

        public void updateMember(IPlayer member)
        {
            members[member.Id] = member;
        }

        public IPlayer? getMemberById(int id)
        {
            return members.TryGetValue(id, out var d) ? d : null;
        }

        public ICollection<IPlayer> getMembers()
        {
            return members.Values.ToList();
        }

        public List<IPlayer> getPartyMembersOnline()
        {
            return getMembers().Where(x => x.IsOnlined).ToList();
        }

        // used whenever entering PQs: will draw every party member that can attempt a target PQ while ingnoring those unfit.
        public ICollection<IPlayer> getEligibleMembers()
        {
            return pqMembers.ToList();
        }

        public void setEligibleMembers(List<IPlayer> eliParty)
        {
            pqMembers = eliParty;
        }

        public int getId()
        {
            return id;
        }

        public void setId(int id)
        {
            this.id = id;
        }

        public int getLeaderId()
        {
            return leaderId;
        }

        public IPlayer getLeader()
        {
            return getMemberById(leaderId) ?? throw new BusinessException();
        }

        public ITeam? getEnemy()
        {
            return enemy;
        }

        public void setEnemy(ITeam? enemy)
        {
            this.enemy = enemy;
        }

        public List<int> getMembersSortedByHistory()
        {
            List<KeyValuePair<int, int>> histList;

            Monitor.Enter(lockObj);
            try
            {
                histList = new(histMembers);
            }
            finally
            {
                Monitor.Exit(lockObj);
            }

            histList.Sort((o1, o2) => (o1.Value).CompareTo(o2.Value));
            return histList.Select(x => x.Key).ToList();
        }

        public sbyte getPartyDoor(int cid)
        {
            List<int> histList = getMembersSortedByHistory();
            return (sbyte)histList.IndexOf(cid);
        }

        public void addDoor(int owner, Door door)
        {
            this.doors.AddOrUpdate(owner, door);
        }

        public void removeDoor(int owner)
        {
            this.doors.Remove(owner);
        }

        public Dictionary<int, Door> getDoors()
        {
            return new Dictionary<int, Door>(doors);
        }

        public void AssignNewLeader()
        {
            var newLeadr = getMembers().Where(x => x.Id != leaderId).OrderByDescending(x => x.Level).FirstOrDefault();
            if (newLeadr != null)
            {
                WorldServer.updateParty(this.getId(), PartyOperation.CHANGE_LEADER, newLeadr);
            }
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + id;
            return result;
        }

        public IPlayer GetRandomMember()
        {
            var allMembers = getMembers();
            return Randomizer.Select(allMembers);
        }

        public override bool Equals(object? obj)
        {
            return obj is Team other && other.getId() == id;
        }
    }
}
