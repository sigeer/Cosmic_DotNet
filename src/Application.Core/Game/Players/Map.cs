using Application.Core.Game.Maps;
using Application.Core.Game.Trades;
using client.inventory;
using constants.id;
using net.packet;
using net.server;
using net.server.world;
using server;
using server.life;
using server.maps;
using tools;

namespace Application.Core.Game.Players
{
    public partial class Player : IMapPlayer
    {
        private Dictionary<int, string> entered = new();
        private int newWarpMap = -1;
        private bool canWarpMap = true;  //only one "warp" must be used per call, and this will define the right one.
        private int canWarpCounter = 0;     //counts how many times "inner warps" have been called.

        /// <summary>
        /// js在调用
        /// </summary>
        /// <param name="PmapId"></param>
        public void setMap(int PmapId)
        {
            this.Map = PmapId;
            if (IsOnlined)
                base.setMap(MapManager.getMap(Map));
        }

        public void setMapId(int mapid)
        {
            this.Map = mapid;
            if (IsOnlined)
                base.setMap(MapManager.getMap(Map));
        }

        public override void setMap(IMap map)
        {
            this.Map = map.getId();
            base.setMap(map);
        }


        public IMap getWarpMap(int map)
        {
            IMap warpMap;
            var eim = getEventInstance();
            if (eim != null)
            {
                warpMap = eim.getMapInstance(map);
            }
            else if (this.getMonsterCarnival() != null && this.getMonsterCarnival()!.getEventMap().getId() == map)
            {
                warpMap = this.getMonsterCarnival()!.getEventMap();
            }
            else
            {
                warpMap = Client.getChannelServer().getMapFactory().getMap(map);
            }
            return warpMap;
        }

        // for use ONLY inside OnUserEnter map scripts that requires a player to change map while still moving between maps.
        public void warpAhead(int map)
        {
            newWarpMap = map;
        }

        private void eventChangedMap(int map)
        {
            var eim = getEventInstance();
            if (eim != null)
            {
                eim.changedMap(this, map);
            }
        }

        private void eventAfterChangedMap(int map)
        {
            var eim = getEventInstance();
            if (eim != null)
            {
                eim.afterChangedMap(this, map);
            }
        }

        public void changeMapBanish(BanishInfo banishInfo)
        {
            if (banishInfo.msg != null)
            {
                dropMessage(5, banishInfo.msg);
            }

            IMap map_ = getWarpMap(Map);
            var portal_ = map_.getPortal(banishInfo.portal);
            changeMap(map_, portal_ != null ? portal_ : map_.getRandomPlayerSpawnpoint());
        }

        public void changeMap(int map)
        {
            IMap warpMap;
            var eim = getEventInstance();

            if (eim != null)
            {
                warpMap = eim.getMapInstance(map);
            }
            else
            {
                warpMap = Client.getChannelServer().getMapFactory().getMap(map);
            }

            changeMap(warpMap, warpMap.getRandomPlayerSpawnpoint());
        }

        public void changeMap(int map, int portal)
        {
            IMap warpMap;
            var eim = getEventInstance();

            if (eim != null)
            {
                warpMap = eim.getMapInstance(map);
            }
            else
            {
                warpMap = Client.getChannelServer().getMapFactory().getMap(map);
            }

            changeMap(warpMap, warpMap.getPortal(portal));
        }

        public void changeMap(int map, string portal)
        {
            IMap warpMap;
            var eim = getEventInstance();

            if (eim != null)
            {
                warpMap = eim.getMapInstance(map);
            }
            else
            {
                warpMap = Client.getChannelServer().getMapFactory().getMap(map);
            }

            changeMap(warpMap, warpMap.getPortal(portal));
        }

        public void changeMap(int map, Portal portal)
        {
            IMap warpMap;
            var eim = getEventInstance();

            if (eim != null)
            {
                warpMap = eim.getMapInstance(map);
            }
            else
            {
                warpMap = Client.getChannelServer().getMapFactory().getMap(map);
            }

            changeMap(warpMap, portal);
        }

        public void changeMap(IMap to, int portal = 0)
        {
            changeMap(to, to.getPortal(portal));
        }

        public void changeMap(IMap target, Portal? pto)
        {
            canWarpCounter++;

            eventChangedMap(target.getId());    // player can be dropped from an event here, hence the new warping target.
            IMap to = getWarpMap(target.getId());
            if (pto == null)
            {
                pto = to.getPortal(0)!;
            }
            changeMapInternal(to, pto.getPosition(), PacketCreator.getWarpToMap(to, pto.getId(), this));
            canWarpMap = false;

            canWarpCounter--;
            if (canWarpCounter == 0)
            {
                canWarpMap = true;
            }

            eventAfterChangedMap(this.getMapId());
        }

        public void changeMap(IMap target, Point pos)
        {
            canWarpCounter++;

            eventChangedMap(target.getId());
            IMap to = getWarpMap(target.getId());
            changeMapInternal(to, pos, PacketCreator.getWarpToMap(to, 0x80, pos, this));
            canWarpMap = false;

            canWarpCounter--;
            if (canWarpCounter == 0)
            {
                canWarpMap = true;
            }

            eventAfterChangedMap(this.getMapId());
        }

        private void changeMapInternal(IMap to, Point pos, Packet warpPacket)
        {
            if (!canWarpMap)
            {
                return;
            }

            this.mapTransitioning.Set(true);

            this.unregisterChairBuff();
            getTrade()?.CancelTrade(TradeResult.UNSUCCESSFUL_ANOTHER_MAP);
            this.closePlayerInteractions();

            var k = this.getParty()?.getEnemy();

            sendPacket(warpPacket);
            MapModel.removePlayer(this);
            if (isLoggedinWorld())
            {
                setMap(to);
                setPosition(pos);
                MapModel.addPlayer(this);
                visitMap(base.MapModel);

                Monitor.Enter(prtLock);
                try
                {
                    if (TeamModel != null)
                    {
                        sendPacket(PacketCreator.updateParty(Client.getChannel(), TeamModel, PartyOperation.SILENT_UPDATE, this));
                        updatePartyMemberHPInternal();
                    }
                }
                finally
                {
                    Monitor.Exit(prtLock);
                }
                if (this.getParty() != null)
                {
                    this.getParty()!.setEnemy(k);
                }
                silentPartyUpdateInternal(getParty());  // EIM script calls inside
            }
            else
            {
                Log.Warning("Chr {CharacterName} got stuck when moving to map {MapId}", getName(), MapModel.getId());
                Client.disconnect(true, false);     // thanks BHB for noticing a player storage stuck case here
                return;
            }

            notifyMapTransferToPartner(MapModel.getId());

            //alas, new map has been specified when a warping was being processed...
            if (newWarpMap != -1)
            {
                canWarpMap = true;

                int temp = newWarpMap;
                newWarpMap = -1;
                changeMap(temp);
            }
            else
            {
                // if this event map has a gate already opened, render it
                var eim = getEventInstance();
                if (eim != null)
                {
                    eim.recoverOpenedGate(this, MapModel.getId());
                }

                // if this map has obstacle components moving, make it do so for this Client
                sendPacket(PacketCreator.environmentMoveList(MapModel.getEnvironment()));
            }
        }

        public bool isChangingMaps()
        {
            return this.mapTransitioning.Get();
        }

        public void setMapTransitionComplete()
        {
            this.mapTransitioning.Set(false);
        }


        public void forceChangeMap(IMap target, Portal? pto)
        {
            // will actually enter the map given as parameter, regardless of being an eventmap or whatnot

            canWarpCounter++;
            eventChangedMap(MapId.NONE);

            var mapEim = target.getEventInstance();
            if (mapEim != null)
            {
                var playerEim = this.getEventInstance();
                if (playerEim != null)
                {
                    playerEim.exitPlayer(this);
                    if (playerEim.getPlayerCount() == 0)
                    {
                        playerEim.dispose();
                    }
                }

                // thanks Thora for finding an issue with players not being actually warped into the target event map (rather sent to the event starting map)
                mapEim.registerPlayer(this, false);
            }

            IMap to = target; // warps directly to the target intead of the target's map id, this allows GMs to patrol players inside instances.
            if (pto == null)
            {
                pto = to.getPortal(0)!;
            }
            changeMapInternal(to, pto.getPosition(), PacketCreator.getWarpToMap(to, pto.getId(), this));
            canWarpMap = false;

            canWarpCounter--;
            if (canWarpCounter == 0)
            {
                canWarpMap = true;
            }

            eventAfterChangedMap(this.getMapId());
        }

        private int getVisitedMapIndex(IMap map)
        {
            int idx = 0;

            foreach (WeakReference<IMap> mapRef in lastVisitedMaps)
            {
                if (mapRef.TryGetTarget(out var d) && map == d)
                    return idx;

                idx++;
            }

            return -1;
        }

        public void enteredScript(string script, int mapid)
        {
            if (!entered.ContainsKey(mapid))
            {
                entered.Add(mapid, script);
            }
        }

        public void visitMap(IMap map)
        {
            Monitor.Enter(petLock);
            try
            {
                int idx = getVisitedMapIndex(map);

                if (idx == -1)
                {
                    if (lastVisitedMaps.Count == YamlConfig.config.server.MAP_VISITED_SIZE)
                    {
                        lastVisitedMaps.RemoveAt(0);
                    }
                }
                else
                {
                    var mapRef = lastVisitedMaps.remove(idx);
                    lastVisitedMaps.Add(mapRef);
                    return;
                }

                lastVisitedMaps.Add(new(map));
            }
            finally
            {
                Monitor.Exit(petLock);
            }
        }

        public void setOwnedMap(IMap? map)
        {
            ownedMap = new(map);
        }

        public IMap? getOwnedMap()
        {
            if (ownedMap.TryGetTarget(out var d))
            {
                return d;

            }
            return null;
        }

        public void startMapEffect(string msg, int itemId)
        {
            startMapEffect(msg, itemId, 30000);
        }

        public void startMapEffect(string msg, int itemId, int duration)
        {
            MapEffect mapEffect = new MapEffect(msg, itemId);
            sendPacket(mapEffect.makeStartData());
            TimerManager.getInstance().schedule(() =>
            {
                sendPacket(mapEffect.makeDestroyData());

            }, duration);
        }

        public void showMapOwnershipInfo(IPlayer mapOwner)
        {
            long curTime = Server.getInstance().getCurrentTime();
            if (nextWarningTime < curTime)
            {
                nextWarningTime = (long)(curTime + TimeSpan.FromMinutes(1).TotalMilliseconds); // show underlevel info again after 1 minute

                string medal = "";
                Item? medalItem = mapOwner.getInventory(InventoryType.EQUIPPED).getItem(-49);
                if (medalItem != null)
                {
                    medal = "<" + ItemInformationProvider.getInstance().getName(medalItem.getItemId()) + "> ";
                }

                List<string> strLines = new();
                strLines.Add("");
                strLines.Add("");
                strLines.Add("");
                strLines.Insert(this.getClient().getChannelServer().getServerMessage().Count() == 0 ? 0 : 1, "Get off my lawn!!");

                this.sendPacket(PacketCreator.getAvatarMega(mapOwner, medal, this.getClient().getChannel(), ItemId.ROARING_TIGER_MESSENGER, strLines, true));
            }
        }
    }
}
