using Application.Core.Game.Maps;
using Application.Core.Game.Relation;
using Application.Core.Managers;
using server.maps;
using tools;

namespace Application.Core.Game.Players
{
    public partial class Player
    {
        private ITeam? teamModel;
        public ITeam? TeamModel
        {
            get
            {
                return teamModel;
            }
            private set
            {
                teamModel = value;
                if (teamModel == null)
                {
                    doorSlot = -1;
                    Party = 0;
                }
                Party = teamModel?.getId() ?? 0;
            }
        }

        /// <summary>
        /// 功能未知
        /// </summary>
        sbyte doorSlot = -1;

        public void setParty(ITeam? p)
        {
            Monitor.Enter(prtLock);
            try
            {
                if (p == null)
                {
                    doorSlot = -1;

                    TeamModel = null;
                }
                else
                {
                    TeamModel = p;
                }
            }
            finally
            {
                Monitor.Exit(prtLock);
            }
        }
        public ITeam? getParty()
        {
            Monitor.Enter(prtLock);
            try
            {
                return TeamModel;
            }
            finally
            {
                Monitor.Exit(prtLock);
            }
        }

        public int getPartyId()
        {
            Monitor.Enter(prtLock);
            try
            {
                return TeamModel?.getId() ?? -1;
            }
            finally
            {
                Monitor.Exit(prtLock);
            }
        }

        public List<IPlayer> getPartyMembersOnline()
        {
            Monitor.Enter(prtLock);
            try
            {
                return TeamModel?.getPartyMembersOnline() ?? new List<IPlayer>();
            }
            finally
            {
                Monitor.Exit(prtLock);
            }
        }

        public List<IPlayer> getPartyMembersOnSameMap()
        {
            List<IPlayer> list = new();
            int thisMapHash = this.MapModel.GetHashCode();

            Monitor.Enter(prtLock);
            try
            {
                if (TeamModel != null)
                {
                    foreach (var chr in TeamModel.getMembers())
                    {
                        var chrMap = chr.getMap();
                        // 用hashcode判断地图是否相同？ -- 同一频道、同一mapid
                        if (chrMap != null && chrMap.GetHashCode() == thisMapHash && chr.isLoggedinWorld())
                        {
                            list.Add(chr);
                        }
                    }
                }
            }
            finally
            {
                Monitor.Exit(prtLock);
            }

            return list;
        }

        public bool isPartyMember(IPlayer chr)
        {
            return isPartyMember(chr.Id);
        }

        public bool isPartyMember(int cid)
        {
            Monitor.Enter(prtLock);
            try
            {
                return TeamModel?.getMemberById(cid) != null;
            }
            finally
            {
                Monitor.Exit(prtLock);
            }
        }

        public void updatePartyMemberHP()
        {
            Monitor.Enter(prtLock);
            try
            {
                updatePartyMemberHPInternal();
            }
            finally
            {
                Monitor.Exit(prtLock);
            }
        }

        private void updatePartyMemberHPInternal()
        {
            if (TeamModel != null)
            {
                int curmaxhp = getCurrentMaxHp();
                int curhp = getHp();
                foreach (IPlayer partychar in this.getPartyMembersOnSameMap())
                {
                    partychar.sendPacket(PacketCreator.updatePartyMemberHP(getId(), curhp, curmaxhp));
                }
            }
        }

        public bool leaveParty()
        {
            ITeam? party;
            bool partyLeader;

            Monitor.Enter(prtLock);
            try
            {
                party = getParty();
                partyLeader = isPartyLeader();
            }
            finally
            {
                Monitor.Exit(prtLock);
            }

            if (party != null)
            {
                if (partyLeader)
                {
                    party.AssignNewLeader();
                }
                TeamManager.LeaveParty(party, this);

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool isPartyLeader()
        {
            Monitor.Enter(prtLock);
            try
            {
                return TeamModel?.getLeaderId() == getId();
            }
            finally
            {
                Monitor.Exit(prtLock);
            }
        }


        public int getDoorSlot()
        {
            if (doorSlot != -1)
            {
                return doorSlot;
            }
            return fetchDoorSlot();
        }

        public int fetchDoorSlot()
        {
            Monitor.Enter(prtLock);
            try
            {
                doorSlot = TeamModel?.getPartyDoor(this.getId()) ?? 0;
                return doorSlot;
            }
            finally
            {
                Monitor.Exit(prtLock);
            }
        }

        public void partyOperationUpdate(ITeam party, List<IPlayer>? exPartyMembers)
        {
            List<WeakReference<IMap>> mapids;

            Monitor.Enter(petLock);
            try
            {
                mapids = new(lastVisitedMaps);
            }
            finally
            {
                Monitor.Exit(petLock);
            }

            List<IPlayer> partyMembers = new();
            foreach (var mc in (exPartyMembers != null) ? exPartyMembers : this.getPartyMembersOnline())
            {
                if (mc.isLoggedinWorld())
                {
                    partyMembers.Add(mc);
                }
            }

            IPlayer? partyLeaver = null;
            if (exPartyMembers != null)
            {
                partyMembers.Remove(this);
                partyLeaver = this;
            }

            IMap map = MapModel;
            List<MapItem>? partyItems = null;

            int partyId = exPartyMembers != null ? -1 : this.getPartyId();
            foreach (var mapRef in mapids)
            {
                if (mapRef.TryGetTarget(out var mapObj))
                {
                    List<MapItem> partyMapItems = mapObj.updatePlayerItemDropsToParty(partyId, Id, partyMembers, partyLeaver);
                    if (MapModel.GetHashCode() == mapObj.GetHashCode())
                    {
                        partyItems = partyMapItems;
                    }
                }
            }

            if (partyItems != null && exPartyMembers == null)
            {
                MapModel.updatePartyItemDropsToNewcomer(this, partyItems);
            }

            updatePartyTownDoors(party, this, partyLeaver, partyMembers);
        }

        private static void addPartyPlayerDoor(IPlayer target)
        {
            var targetDoor = target.getPlayerDoor();
            if (targetDoor != null)
            {
                target.applyPartyDoor(targetDoor, true);
            }
        }

        private static void removePartyPlayerDoor(ITeam party, IPlayer target)
        {
            target.removePartyDoor(party);
        }


        private static void updatePartyTownDoors(ITeam party, IPlayer target, IPlayer? partyLeaver, List<IPlayer> partyMembers)
        {
            if (partyLeaver != null)
            {
                removePartyPlayerDoor(party, target);
            }
            else
            {
                addPartyPlayerDoor(target);
            }

            Dictionary<int, Door>? partyDoors = null;
            if (partyMembers.Count > 0)
            {
                partyDoors = party.getDoors();

                foreach (IPlayer pchr in partyMembers)
                {
                    Door? door = partyDoors.GetValueOrDefault(pchr.getId());
                    if (door != null)
                    {
                        door.updateDoorPortal(pchr);
                    }
                }

                foreach (Door door in partyDoors.Values)
                {
                    foreach (IPlayer pchar in partyMembers)
                    {
                        DoorObject mdo = door.getTownDoor();
                        mdo.sendDestroyData(pchar.Client, true);
                        pchar.removeVisibleMapObject(mdo);
                    }
                }

                if (partyLeaver != null)
                {
                    var leaverDoors = partyLeaver.getDoors();
                    foreach (Door door in leaverDoors)
                    {
                        foreach (IPlayer pchar in partyMembers)
                        {
                            DoorObject mdo = door.getTownDoor();
                            mdo.sendDestroyData(pchar.Client, true);
                            pchar.removeVisibleMapObject(mdo);
                        }
                    }
                }

                List<int> histMembers = party.getMembersSortedByHistory();
                foreach (int chrid in histMembers)
                {
                    Door? door = partyDoors.GetValueOrDefault(chrid);
                    if (door != null)
                    {
                        foreach (IPlayer pchar in partyMembers)
                        {
                            DoorObject mdo = door.getTownDoor();
                            mdo.sendSpawnData(pchar.Client);
                            pchar.addVisibleMapObject(mdo);
                        }
                    }
                }
            }

            if (partyLeaver != null)
            {
                var leaverDoors = partyLeaver.getDoors();

                if (partyDoors != null)
                {
                    foreach (Door door in partyDoors.Values)
                    {
                        DoorObject mdo = door.getTownDoor();
                        mdo.sendDestroyData(partyLeaver.Client, true);
                        partyLeaver.removeVisibleMapObject(mdo);
                    }
                }

                foreach (Door door in leaverDoors)
                {
                    DoorObject mdo = door.getTownDoor();
                    mdo.sendDestroyData(partyLeaver.Client, true);
                    partyLeaver.removeVisibleMapObject(mdo);
                }

                foreach (Door door in leaverDoors)
                {
                    door.updateDoorPortal(partyLeaver);

                    DoorObject mdo = door.getTownDoor();
                    mdo.sendSpawnData(partyLeaver.Client);
                    partyLeaver.addVisibleMapObject(mdo);
                }
            }
        }

        public void applyPartyDoor(Door door, bool partyUpdate)
        {
            ITeam? chrParty;
            Monitor.Enter(prtLock);
            try
            {
                if (!partyUpdate)
                {
                    pdoor = door;
                }

                chrParty = getParty();
                if (chrParty != null)
                {
                    chrParty.addDoor(Id, door);
                }
            }
            finally
            {
                Monitor.Exit(prtLock);
            }

            silentPartyUpdateInternal(chrParty);
        }

        public Door? removePartyDoor(bool partyUpdate)
        {
            Door? ret = null;
            ITeam? chrParty;

            Monitor.Enter(prtLock);
            try
            {
                chrParty = getParty();
                if (chrParty != null)
                {
                    chrParty.removeDoor(Id);
                }

                if (!partyUpdate)
                {
                    ret = pdoor;
                    pdoor = null;
                }
            }
            finally
            {
                Monitor.Exit(prtLock);
            }

            silentPartyUpdateInternal(chrParty);
            return ret;
        }

        public void removePartyDoor(ITeam formerParty)
        {
            // player is no longer registered at this party
            formerParty.removeDoor(Id);
        }
    }
}
