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


using Application.Core.Game.Life;
using Application.Core.Game.Maps;
using Application.Core.Game.Relation;
using Application.Core.Game.Skills;
using Application.Core.model;
using Application.Core.scripting.Event;
using client;
using constants.inventory;
using net.server.coordinator.world;
using scripting.Event.scheduler;
using server;
using server.expeditions;
using server.life;
using server.maps;
using System.Reflection;
using tools;

namespace scripting.Event;

/**
 * @author Matze
 * @author Ronan
 */
public class EventInstanceManager
{
    protected ILogger log = LogFactory.GetLogger("EventInstanceManger");
    private Dictionary<int, IPlayer> chars = new();
    private int leaderId = -1;
    private List<Monster> mobs = new();
    private Dictionary<IPlayer, int> killCount = new();
    private EventManager em;
    private EventScriptScheduler ess;
    private MapManager mapManager;
    private string name;
    private Dictionary<string, object> props = new();
    private Dictionary<string, object> objectProps = new();
    private long timeStarted = 0;
    private long eventTime = 0;
    private Expedition? expedition = null;
    private List<int> mapIds = new();

    private ReaderWriterLockSlim lockObj = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

    private object propertyLock = new object();
    private object scriptLock = new object();

    private ScheduledFuture? event_schedule = null;
    private bool disposed = false;
    private bool eventCleared = false;
    private bool eventStarted = false;

    // multi-leveled PQ rewards!
    Dictionary<int, EventRewardsPair> eventRewards = new Dictionary<int, EventRewardsPair>(YamlConfig.config.server.MAX_EVENT_LEVELS);

    // Exp/Meso rewards by CLEAR on a stage
    private List<int> onMapClearExp = new();
    private List<int> onMapClearMeso = new();

    // registers player status on an event (null on this Map structure equals to 0)
    private Dictionary<int, int> playerGrid = new();

    // registers all opened gates on the event. Will help late characters to encounter next stages gates already opened
    private Dictionary<int, KeyValuePair<string, int>?> openedGates = new();

    // forces deletion of items not supposed to be held outside of the event, dealt on a player's leaving moment.
    private HashSet<int> exclusiveItems = new();

    public EventInstanceManager(EventManager em, string name)
    {
        this.em = em;
        this.name = name;
        this.ess = new EventScriptScheduler();
        this.mapManager = new MapManager(this, em.getWorldServer().getId(), em.getChannelServer().getId());
    }

    public void setName(string name)
    {
        this.name = name;
    }

    public EventManager getEm()
    {
        Monitor.Enter(scriptLock);
        try
        {
            return em;
        }
        finally
        {
            Monitor.Exit(scriptLock);
        }
    }

    public int getEventPlayersJobs()
    {
        //Bits -> 0: BEGINNER 1: WARRIOR 2: MAGICIAN
        //        3: BOWMAN 4: THIEF 5: PIRATE

        int mask = 0;
        foreach (var chr in getPlayers())
        {
            mask |= (1 << chr.getJob().getJobNiche());
        }

        return mask;
    }

    public void applyEventPlayersItemBuff(int itemId)
    {
        List<IPlayer> players = getPlayerList();
        var mse = ItemInformationProvider.getInstance().getItemEffect(itemId);

        if (mse != null)
        {
            foreach (IPlayer player in players)
            {
                mse.applyTo(player);
            }
        }
    }

    public void applyEventPlayersSkillBuff(int skillId)
    {
        applyEventPlayersSkillBuff(skillId, int.MaxValue);
    }

    public void applyEventPlayersSkillBuff(int skillId, int skillLv)
    {
        List<IPlayer> players = getPlayerList();
        var skill = SkillFactory.getSkill(skillId);

        if (skill != null)
        {
            StatEffect mse = skill.getEffect(Math.Min(skillLv, skill.getMaxLevel()));
            if (mse != null)
            {
                foreach (IPlayer player in players)
                {
                    mse.applyTo(player);
                }
            }
        }
    }

    public void giveEventPlayersExp(int gain, int mapId = -1)
    {
        if (gain == 0)
        {
            return;
        }

        List<IPlayer> players = getPlayerList();

        if (mapId == -1)
        {
            foreach (IPlayer mc in players)
            {
                mc.gainExp(gain * mc.getExpRate(), true, true);
            }
        }
        else
        {
            foreach (IPlayer mc in players)
            {
                if (mc.getMapId() == mapId)
                {
                    mc.gainExp(gain * mc.getExpRate(), true, true);
                }
            }
        }
    }


    public void giveEventPlayersMeso(int gain, int mapId = -1)
    {
        if (gain == 0)
        {
            return;
        }

        List<IPlayer> players = getPlayerList();

        if (mapId == -1)
        {
            foreach (IPlayer mc in players)
            {
                mc.gainMeso(gain * mc.getMesoRate());
            }
        }
        else
        {
            foreach (IPlayer mc in players)
            {
                if (mc.getMapId() == mapId)
                {
                    mc.gainMeso(gain * mc.getMesoRate());
                }
            }
        }

    }

    public object? invokeScriptFunction(string name, params object[] args)
    {
        if (!disposed)
        {
            return em.getIv().CallFunction(name, args);
        }
        else
        {
            return null;
        }
    }

    object registerLock = new object();

    public void registerPlayer(IPlayer chr, bool runEntryScript = true)
    {

        lock (registerLock)
        {
            if (chr == null || !chr.isLoggedinWorld() || disposed)
            {
                return;
            }

            lockObj.EnterWriteLock();
            try
            {
                if (chars.ContainsKey(chr.getId()))
                {
                    return;
                }

                chars.AddOrUpdate(chr.getId(), chr);
                chr.setEventInstance(this);
            }
            finally
            {
                lockObj.ExitWriteLock();
            }

            if (runEntryScript)
            {
                try
                {
                    invokeScriptFunction("playerEntry", this, chr);
                }
                catch (Exception ex)
                {
                    log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "playerEntry", em.getName());
                }
            }
        }

    }

    public void exitPlayer(IPlayer chr)
    {
        if (chr == null || !chr.isLoggedin())
        {
            return;
        }

        unregisterPlayer(chr);

        try
        {
            invokeScriptFunction("playerExit", this, chr);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "playerExit", em.getName());
        }
    }

    public void dropMessage(int type, string message)
    {
        foreach (IPlayer chr in getPlayers())
        {
            chr.dropMessage(type, message);
        }
    }

    public void restartEventTimer(long time)
    {
        stopEventTimer();
        startEventTimer(time);
    }

    public void startEventTimer(long time)
    {
        timeStarted = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        eventTime = time;

        foreach (IPlayer chr in getPlayers())
        {
            chr.sendPacket(PacketCreator.getClock((int)(time / 1000)));
        }

        event_schedule = TimerManager.getInstance().schedule(() =>
        {
            dismissEventTimer();

            try
            {
                invokeScriptFunction("scheduledTimeout", this);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Event script {ScriptName} does not implement the scheduledTimeout function", em.getName());
            }
        }, time);
    }

    public void addEventTimer(long time)
    {
        if (event_schedule != null)
        {
            if (event_schedule.cancel(false))
            {
                long nextTime = getTimeLeft() + time;
                eventTime += time;

                event_schedule = TimerManager.getInstance().schedule(() =>
                {
                    dismissEventTimer();

                    try
                    {
                        invokeScriptFunction("scheduledTimeout", this);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, "Event script {ScriptName} does not implement the scheduledTimeout function", em.getName());
                    }
                }, nextTime);
            }
        }
        else
        {
            startEventTimer(time);
        }
    }

    private void dismissEventTimer()
    {
        foreach (IPlayer chr in getPlayers())
        {
            chr.sendPacket(PacketCreator.removeClock());
        }

        event_schedule = null;
        eventTime = 0;
        timeStarted = 0;
    }

    public void stopEventTimer()
    {
        if (event_schedule != null)
        {
            event_schedule.cancel(false);
            event_schedule = null;
        }

        dismissEventTimer();
    }

    public bool isTimerStarted()
    {
        return eventTime > 0 && timeStarted > 0;
    }

    public long getTimeLeft()
    {
        return eventTime - (DateTimeOffset.Now.ToUnixTimeMilliseconds() - timeStarted);
    }

    public void registerParty(IPlayer chr)
    {
        if (chr.isPartyLeader())
        {
            registerParty(chr.getParty()!, chr.getMap());
        }
    }

    public void registerParty(ITeam party, IMap map)
    {
        foreach (var mpc in party.getEligibleMembers())
        {
            if (mpc.IsOnlined)
            {
                // thanks resinate
                var chr = map.getCharacterById(mpc.getId());
                if (chr != null)
                {
                    registerPlayer(chr);
                }
            }
        }
    }

    public void registerExpedition(Expedition exped)
    {
        expedition = exped;
        registerExpeditionTeam(exped, exped.getRecruitingMap().getId());
    }

    private void registerExpeditionTeam(Expedition exped, int recruitMap)
    {
        expedition = exped;

        foreach (IPlayer chr in exped.getActiveMembers())
        {
            if (chr.getMapId() == recruitMap)
            {
                registerPlayer(chr);
            }
        }
    }

    public void unregisterPlayer(IPlayer chr)
    {
        try
        {
            invokeScriptFunction("playerUnregistered", this, chr);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Event script {ScriptName} does not implement the playerUnregistered function", em.getName());
        }

        lockObj.EnterWriteLock();
        try
        {
            chars.Remove(chr.getId());
            chr.setEventInstance(null);
        }
        finally
        {
            lockObj.ExitWriteLock();
        }

        gridRemove(chr);
        dropExclusiveItems(chr);
    }

    public int getPlayerCount()
    {
        lockObj.EnterReadLock();
        try
        {
            return chars.Count;
        }
        finally
        {
            lockObj.ExitReadLock();
        }
    }

    public IPlayer? getPlayerById(int id)
    {
        lockObj.EnterReadLock();
        try
        {
            return chars.GetValueOrDefault(id);
        }
        finally
        {
            lockObj.ExitReadLock();
        }
    }

    public List<IPlayer> getPlayers()
    {
        lockObj.EnterReadLock();
        try
        {
            return new(chars.Values);
        }
        finally
        {
            lockObj.ExitReadLock();
        }
    }

    private List<IPlayer> getPlayerList()
    {
        lockObj.EnterReadLock();
        try
        {
            return chars.Values.ToList();
        }
        finally
        {
            lockObj.ExitReadLock();
        }
    }

    public void registerMonster(Monster mob)
    {
        if (!mob.getStats().isFriendly())
        { //We cannot register moon bunny
            mobs.Add(mob);
        }
    }

    public void movePlayer(IPlayer chr)
    {
        try
        {
            invokeScriptFunction("moveMap", this, chr);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "moveMap", em.getName());
        }
    }

    public void changedMap(IPlayer chr, int mapId)
    {
        try
        {
            invokeScriptFunction("changedMap", this, chr, mapId);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "changedMap", em.getName());
        } // optional
    }

    public void afterChangedMap(IPlayer chr, int mapId)
    {
        try
        {
            invokeScriptFunction("afterChangedMap", this, chr, mapId);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "afterChangedMap", em.getName());
        } // optional
    }

    object changeLeaderLock = new object();
    public void changedLeader(IPlayer ldr)
    {
        lock (changeLeaderLock)
        {
            try
            {
                invokeScriptFunction("changedLeader", this, ldr);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "changedLeader", em.getName());
            }

            leaderId = ldr.getId();
        }

    }

    public void monsterKilled(Monster mob, bool hasKiller)
    {
        int scriptResult = 0;

        Monitor.Enter(scriptLock);
        try
        {
            mobs.Remove(mob);

            if (eventStarted)
            {
                scriptResult = 1;

                if (mobs.Count == 0)
                {
                    scriptResult = 2;
                }
            }
        }
        finally
        {
            Monitor.Exit(scriptLock);
        }

        if (scriptResult > 0)
        {
            try
            {
                invokeScriptFunction("monsterKilled", mob, this, hasKiller);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "monsterKilled", em.getName());
            }

            if (scriptResult > 1)
            {
                try
                {
                    invokeScriptFunction("allMonstersDead", this, hasKiller);
                }
                catch (Exception ex)
                {
                    log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "allMonstersDead", em.getName());
                }
            }
        }
    }

    public void friendlyKilled(Monster mob, bool hasKiller)
    {
        try
        {
            invokeScriptFunction("friendlyKilled", mob, this, hasKiller);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "friendlyKilled", em.getName());
        } //optional
    }

    public void friendlyDamaged(Monster mob)
    {
        try
        {
            invokeScriptFunction("friendlyDamaged", this, mob);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "friendlyDamaged", em.getName());
        } // optional
    }

    public void friendlyItemDrop(Monster mob)
    {
        try
        {
            invokeScriptFunction("friendlyItemDrop", this, mob);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "friendlyItemDrop", em.getName());
        } // optional
    }

    public void playerKilled(IPlayer chr)
    {
        ThreadManager.getInstance().newTask(() =>
        {
            try
            {
                invokeScriptFunction("playerDead", this, chr);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "playerDead", em.getName());
            } // optional
        });
    }

    public void reviveMonster(Monster mob)
    {
        try
        {
            invokeScriptFunction("monsterRevive", this, mob);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "monsterRevive", em.getName());
        } // optional
    }

    public bool revivePlayer(IPlayer chr)
    {
        try
        {
            return Convert.ToBoolean(invokeScriptFunction("playerRevive", this, chr));
        }
        catch (Exception ex)
        {
            log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "playerRevive", em.getName());
        } // optional

        return true;
    }

    public void playerDisconnected(IPlayer chr)
    {
        try
        {
            invokeScriptFunction("playerDisconnected", this, chr);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "playerDisconnected", em.getName());
        }

        EventRecallCoordinator.getInstance().storeEventInstance(chr.getId(), this);
    }

    public void monsterKilled(IPlayer chr, Monster mob)
    {
        try
        {
            int inc = Convert.ToInt32(invokeScriptFunction("monsterValue", this, mob.getId()));

            if (inc != 0)
            {
                var kc = killCount.get(chr);
                if (kc == null)
                {
                    kc = inc;
                }
                else
                {
                    kc += inc;
                }
                killCount.AddOrUpdate(chr, kc.Value);
                expedition?.monsterKilled(chr, mob);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "monsterValue", em.getName());
        }
    }

    public int getKillCount(IPlayer chr)
    {
        return killCount.GetValueOrDefault(chr, 0);
    }

    public void dispose()
    {
        lockObj.EnterReadLock();
        try
        {
            foreach (IPlayer chr in chars.Values)
            {
                chr.setEventInstance(null);
            }
        }
        finally
        {
            lockObj.ExitReadLock();
        }

        dispose(false);
    }

    object disposeLock = new object();
    public void dispose(bool shutdown)
    {
        lock (disposeLock)
        {


            // should not trigger any event script method after disposed
            if (disposed)
            {
                return;
            }

            try
            {
                invokeScriptFunction("dispose", this);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "dispose", em.getName());
            }
            disposed = true;

            ess.dispose();

            lockObj.EnterWriteLock();
            try
            {
                foreach (IPlayer chr in chars.Values)
                {
                    chr.setEventInstance(null);
                }
                chars.Clear();
                mobs.Clear();
                // ess = null;
            }
            finally
            {
                lockObj.ExitWriteLock();
            }

            if (event_schedule != null)
            {
                event_schedule.cancel(false);
                event_schedule = null;
            }

            killCount.Clear();
            mapIds.Clear();
            props.Clear();
            objectProps.Clear();

            disposeExpedition();

            Monitor.Enter(scriptLock);
            try
            {
                if (!eventCleared)
                {
                    em.disposeInstance(name);
                }
            }
            finally
            {
                Monitor.Exit(scriptLock);
            }

            TimerManager.getInstance().schedule(() =>
            {
                mapManager.dispose();   // issues from instantly disposing some event objects found thanks to MedicOP
                //lockObj.EnterWriteLock();
                //try
                //{
                //    mapManager = null;
                //    em = null;
                //}
                //finally
                //{
                //    lockObj.ExitWriteLock();
                //}
            }, TimeSpan.FromMinutes(1));
        }
    }

    public MapManager getMapFactory()
    {
        return mapManager;
    }

    public void schedule(string methodName, long delay)
    {
        lockObj.EnterReadLock();
        try
        {
            if (ess != null)
            {
                var r = () =>
                {
                    try
                    {
                        invokeScriptFunction(methodName, this);
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, "Invoke {JsFunction} from {ScriptName}", methodName, em.getName());
                    }
                };

                ess.registerEntry(r, delay);
            }
        }
        finally
        {
            lockObj.ExitReadLock();
        }
    }

    public string getName()
    {
        return name;
    }

    public IMap getMapInstance(int mapId)
    {
        IMap map = mapManager.getMap(mapId);
        map.setEventInstance(this);

        if (!mapManager.isMapLoaded(mapId))
        {
            Monitor.Enter(scriptLock);
            try
            {
                if (em.getProperty("shuffleReactors") == "true")
                {
                    map.shuffleReactors();
                }
            }
            finally
            {
                Monitor.Exit(scriptLock);
            }
        }
        return map;
    }

    public void setIntProperty(string key, int value)
    {
        setProperty(key, value);
    }

    public void setProperty(string key, int value)
    {
        setProperty(key, value.ToString());
    }

    public void setProperty(string key, string value)
    {
        Monitor.Enter(propertyLock);
        try
        {
            props.AddOrUpdate(key, value);
        }
        finally
        {
            Monitor.Exit(propertyLock);
        }
    }

    public object? setProperty(string key, string value, bool prev)
    {
        Monitor.Enter(propertyLock);
        try
        {
            return props.AddOrUpdateReturnOldValue(key, value);
        }
        finally
        {
            Monitor.Exit(propertyLock);
        }
    }

    public void setObjectProperty(string key, object obj)
    {
        Monitor.Enter(propertyLock);
        try
        {
            objectProps.AddOrUpdate(key, obj);
        }
        finally
        {
            Monitor.Exit(propertyLock);
        }
    }

    public string? getProperty(string key)
    {
        Monitor.Enter(propertyLock);
        try
        {
            var d = props.GetValueOrDefault(key);
            return d?.ToString();
        }
        finally
        {
            Monitor.Exit(propertyLock);
        }
    }

    public int getIntProperty(string key)
    {
        Monitor.Enter(propertyLock);
        try
        {
            var d = props.GetValueOrDefault(key);
            if (d == null)
                return 0;
            return (int)d;
        }
        finally
        {
            Monitor.Exit(propertyLock);
        }
    }

    public object? getObjectProperty(string key)
    {
        Monitor.Enter(propertyLock);
        try
        {
            return objectProps.GetValueOrDefault(key);
        }
        finally
        {
            Monitor.Exit(propertyLock);
        }
    }

    public void leftParty(IPlayer chr)
    {
        try
        {
            invokeScriptFunction("leftParty", this, chr);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "leftParty", em.getName());
        }
    }

    public void disbandParty()
    {
        try
        {
            invokeScriptFunction("disbandParty", this);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "disbandParty", em.getName());
        }
    }

    public void clearPQ()
    {
        try
        {
            invokeScriptFunction("clearPQ", this);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "clearPQ", em.getName());
        }
    }

    public void removePlayer(IPlayer chr)
    {
        try
        {
            invokeScriptFunction("playerExit", this, chr);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "playerExit", em.getName());
        }
    }

    public bool isLeader(IPlayer chr)
    {
        return chr.isPartyLeader();
    }

    public bool isEventLeader(IPlayer chr)
    {
        return (chr.getId() == getLeaderId());
    }

    public IMap? getInstanceMap(int mapid)
    {
        if (disposed)
        {
            return null;
        }
        mapIds.Add(mapid);
        return getMapFactory().getMap(mapid);
    }

    public bool disposeIfPlayerBelow(byte size, int towarp)
    {
        if (disposed)
        {
            return true;
        }
        if (chars == null)
        {
            return false;
        }

        IMap? map = null;
        if (towarp > 0)
        {
            map = this.getMapFactory().getMap(towarp);
        }

        List<IPlayer> players = getPlayerList();

        try
        {
            if (players.Count < size)
            {
                foreach (IPlayer chr in players)
                {
                    if (chr == null)
                    {
                        continue;
                    }

                    unregisterPlayer(chr);
                    if (towarp > 0)
                    {
                        chr.changeMap(map!, map!.getPortal(0));
                    }
                }

                dispose();
                return true;
            }
        }
        catch (Exception ex)
        {
            log.Error(ex.ToString());
        }

        return false;
    }

    public void spawnNpc(int npcId, Point pos, IMap map)
    {
        NPC npc = LifeFactory.getNPC(npcId);
        if (npc != null)
        {
            npc.setPosition(pos);
            npc.setCy(pos.Y);
            npc.setRx0(pos.X + 50);
            npc.setRx1(pos.X - 50);
            npc.setFh(map.getFootholds().findBelow(pos).getId());
            map.addMapObject(npc);
            map.broadcastMessage(PacketCreator.spawnNPC(npc));
        }
    }

    public void dispatchRaiseQuestMobCount(int mobid, int mapid)
    {
        var mapChars = getInstanceMap(mapid)?.getMapPlayers() ?? [];
        if (mapChars.Count > 0)
        {
            List<IPlayer> eventMembers = getPlayers();

            foreach (IPlayer evChr in eventMembers)
            {
                var chr = mapChars.GetValueOrDefault(evChr.getId());

                if (chr != null && chr.isLoggedinWorld())
                {
                    chr.raiseQuestMobCount(mobid);
                }
            }
        }
    }

    public Monster getMonster(int mid)
    {
        return (LifeFactory.GetMonsterTrust(mid));
    }

    private List<int> convertToIntegerList(List<object> objects)
    {
        return objects.Select(x => Convert.ToInt32(x)).ToList();
    }

    public void setEventClearStageExp(List<object> gain)
    {
        onMapClearExp.Clear();
        onMapClearExp.AddRange(convertToIntegerList(gain));
    }

    public void setEventClearStageMeso(List<object> gain)
    {
        onMapClearMeso.Clear();
        onMapClearMeso.AddRange(convertToIntegerList(gain));
    }

    public int getClearStageExp(int stage)
    {
        //stage counts from ONE.
        if (stage > onMapClearExp.Count)
        {
            return 0;
        }
        return onMapClearExp.get(stage - 1);
    }

    public int getClearStageMeso(int stage)
    {
        //stage counts from ONE.
        if (stage > onMapClearMeso.Count)
        {
            return 0;
        }
        return onMapClearMeso.ElementAt(stage - 1);
    }

    public List<int> getClearStageBonus(int stage)
    {
        List<int> list = new();
        list.Add(getClearStageExp(stage));
        list.Add(getClearStageMeso(stage));

        return list;
    }

    private void dropExclusiveItems(IPlayer chr)
    {
        AbstractPlayerInteraction api = chr.getAbstractPlayerInteraction();

        foreach (int item in exclusiveItems)
        {
            api.removeAll(item);
        }
    }

    public void setExclusiveItems(List<object> items)
    {
        List<int> exclusive = convertToIntegerList(items);

        lockObj.EnterWriteLock();
        try
        {
            exclusiveItems.addAll(exclusive);
        }
        finally
        {
            lockObj.ExitWriteLock();
        }
    }

    public void setEventRewards(List<object> rwds, List<object> qtys, int expGiven = 0)
    {
        setEventRewards(1, rwds, qtys, expGiven);
    }

    public void setEventRewards(int eventLevel, List<object> rwds, List<object> qtys, int expGiven = 0)
    {
        // fixed EXP will be rewarded at the same time the random item is given

        if (eventLevel <= 0 || eventLevel > YamlConfig.config.server.MAX_EVENT_LEVELS)
        {
            return;
        }
        eventLevel--;    //event level starts from 1

        List<int> rewardIds = convertToIntegerList(rwds);
        List<int> rewardQtys = convertToIntegerList(qtys);

        //rewardsSet and rewardsQty hold temporary values
        lockObj.EnterWriteLock();
        try
        {
            eventRewards.AddOrUpdate(eventLevel, new EventRewardsPair(rewardIds, rewardQtys, expGiven));
        }
        finally
        {
            lockObj.ExitWriteLock();
        }
    }

    private byte getRewardListRequirements(int level)
    {
        if (level >= eventRewards.Count)
        {
            return 0;
        }

        byte rewardTypes = 0;
        var list = eventRewards.GetValueOrDefault(level)!;

        foreach (int itemId in list.Rewards)
        {
            rewardTypes |= (byte)(1 << (int)ItemConstants.getInventoryType(itemId));
        }

        return rewardTypes;
    }

    private bool hasRewardSlot(IPlayer player, int eventLevel)
    {
        byte listReq = getRewardListRequirements(eventLevel);   //gets all types of items present in the event reward list

        //iterating over all valid inventory types
        for (byte type = 1; type <= 5; type++)
        {
            if ((listReq >> type) % 2 == 1 && !player.hasEmptySlot(type))
            {
                return false;
            }
        }

        return true;
    }

    public bool giveEventReward(IPlayer player)
    {
        return giveEventReward(player, 1);
    }

    //gives out EXP & a random item in a similar fashion of when clearing KPQ, LPQ, etc.
    public bool giveEventReward(IPlayer player, int eventLevel)
    {
        List<int>? rewardsSet, rewardsQty;
        int rewardExp;

        lockObj.EnterReadLock();
        try
        {
            eventLevel--;       //event level starts counting from 1
            if (eventLevel >= eventRewards.Count)
            {
                return true;
            }

            var item = eventRewards.GetValueOrDefault(eventLevel)!;
            rewardsSet = item.Rewards;
            rewardsQty = item.Quantity;

            rewardExp = item.Exp;
        }
        finally
        {
            lockObj.ExitReadLock();
        }

        if (rewardsSet == null || rewardsSet.Count == 0)
        {
            if (rewardExp > 0)
            {
                player.gainExp(rewardExp);
            }
            return true;
        }

        if (!hasRewardSlot(player, eventLevel))
        {
            return false;
        }

        AbstractPlayerInteraction api = player.getAbstractPlayerInteraction();
        int rnd = (int)Math.Floor(Randomizer.nextDouble() * rewardsSet.Count);

        api.gainItem(rewardsSet.get(rnd), (short)rewardsQty.ElementAtOrDefault(rnd));
        if (rewardExp > 0)
        {
            player.gainExp(rewardExp);
        }
        return true;
    }

    private void disposeExpedition()
    {
        if (expedition != null)
        {
            expedition.dispose(eventCleared);

            Monitor.Enter(scriptLock);
            try
            {
                expedition.removeChannelExpedition(em.getChannelServer());
            }
            finally
            {
                Monitor.Exit(scriptLock);
            }

            expedition = null;
        }
    }

    object startEvtLock = new object();
    public void startEvent()
    {
        lock (startEvtLock)
        {
            eventStarted = true;

            try
            {
                invokeScriptFunction("afterSetup", this);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Invoke {JsFunction} from {ScriptName}", "afterSetup", em.getName());
            }
        }
    }

    public void setEventCleared()
    {
        eventCleared = true;

        foreach (IPlayer chr in getPlayers())
        {
            chr.awardQuestPoint(YamlConfig.config.server.QUEST_POINT_PER_EVENT_CLEAR);
        }

        Monitor.Enter(scriptLock);
        try
        {
            em.disposeInstance(name);
        }
        finally
        {
            Monitor.Exit(scriptLock);
        }

        disposeExpedition();
    }

    public bool isEventCleared()
    {
        return eventCleared;
    }

    public bool isEventDisposed()
    {
        return disposed;
    }

    private bool isEventTeamLeaderOn()
    {
        foreach (IPlayer chr in getPlayers())
        {
            if (chr.getId() == getLeaderId())
            {
                return true;
            }
        }

        return false;
    }

    public bool checkEventTeamLacking(bool leavingEventMap, int minPlayers)
    {
        if (eventCleared && getPlayerCount() > 1)
        {
            return false;
        }

        if (!eventCleared && leavingEventMap && !isEventTeamLeaderOn())
        {
            return true;
        }
        return getPlayerCount() < minPlayers;
    }

    public bool isExpeditionTeamLackingNow(bool leavingEventMap, int minPlayers, IPlayer quitter)
    {
        if (eventCleared)
        {
            return leavingEventMap && getPlayerCount() <= 1;
        }
        else
        {
            // thanks Conrad for noticing expeditions don't need to have neither the leader nor meet the minimum requirement inside the event
            return getPlayerCount() <= 1;
        }
    }

    public bool isEventTeamLackingNow(bool leavingEventMap, int minPlayers, IPlayer quitter)
    {
        if (eventCleared)
        {
            return leavingEventMap && getPlayerCount() <= 1;
        }
        else
        {
            if (leavingEventMap && getLeaderId() == quitter.getId())
            {
                return true;
            }
            return getPlayerCount() <= minPlayers;
        }
    }

    public bool isEventTeamTogether()
    {
        lockObj.EnterReadLock();
        try
        {
            if (chars.Count <= 1)
            {
                return true;
            }

            if (chars.Values.Zip(chars.Values.Skip(1), (a, b) => a.getMapId() == b.getMapId()).Any(x => x))
                return false;
            return true;
        }
        finally
        {
            lockObj.ExitReadLock();
        }
    }

    public void warpEventTeam(int warpFrom, int warpTo)
    {
        List<IPlayer> players = getPlayerList();

        foreach (IPlayer chr in players)
        {
            if (chr.getMapId() == warpFrom)
            {
                chr.changeMap(warpTo);
            }
        }
    }

    public void warpEventTeam(int warpTo)
    {
        List<IPlayer> players = getPlayerList();

        foreach (IPlayer chr in players)
        {
            chr.changeMap(warpTo);
        }
    }

    public void warpEventTeamToMapSpawnPoint(int warpFrom, int warpTo, int toSp)
    {
        List<IPlayer> players = getPlayerList();

        foreach (IPlayer chr in players)
        {
            if (chr.getMapId() == warpFrom)
            {
                chr.changeMap(warpTo, toSp);
            }
        }
    }

    public void warpEventTeamToMapSpawnPoint(int warpTo, int toSp)
    {
        List<IPlayer> players = getPlayerList();

        foreach (IPlayer chr in players)
        {
            chr.changeMap(warpTo, toSp);
        }
    }

    public int getLeaderId()
    {
        lockObj.EnterReadLock();
        try
        {
            return leaderId;
        }
        finally
        {
            lockObj.ExitReadLock();
        }
    }

    public IPlayer? getLeader()
    {
        lockObj.EnterReadLock();
        try
        {
            return chars.GetValueOrDefault(leaderId);
        }
        finally
        {
            lockObj.ExitReadLock();
        }
    }

    public void setLeader(IPlayer chr)
    {
        lockObj.EnterWriteLock();
        try
        {
            leaderId = chr.getId();
        }
        finally
        {
            lockObj.ExitWriteLock();
        }
    }

    public void showWrongEffect()
    {
        showWrongEffect(getLeader().getMapId());
    }

    public void showWrongEffect(int mapId)
    {
        IMap map = getMapInstance(mapId);
        map.broadcastMessage(PacketCreator.showEffect("quest/party/wrong_kor"));
        map.broadcastMessage(PacketCreator.playSound("Party1/Failed"));
    }

    public void showClearEffect()
    {
        showClearEffect(false);
    }

    public void showClearEffect(bool hasGate)
    {
        var leader = getLeader();
        if (leader != null)
        {
            showClearEffect(hasGate, leader.getMapId());
        }
    }

    public void showClearEffect(int mapId)
    {
        showClearEffect(false, mapId);
    }

    public void showClearEffect(bool hasGate, int mapId)
    {
        showClearEffect(hasGate, mapId, "gate", 2);
    }

    public void showClearEffect(int mapId, string mapObj, int newState)
    {
        showClearEffect(true, mapId, mapObj, newState);
    }

    public void showClearEffect(bool hasGate, int mapId, string mapObj, int newState)
    {
        IMap map = getMapInstance(mapId);
        map.broadcastMessage(PacketCreator.showEffect("quest/party/clear"));
        map.broadcastMessage(PacketCreator.playSound("Party1/Clear"));
        if (hasGate)
        {
            map.broadcastMessage(PacketCreator.environmentChange(mapObj, newState));
            lockObj.EnterWriteLock();
            try
            {
                openedGates.AddOrUpdate(map.getId(), new(mapObj, newState));
            }
            finally
            {
                lockObj.ExitWriteLock();
            }
        }
    }

    public void recoverOpenedGate(IPlayer chr, int thisMapId)
    {
        KeyValuePair<string, int>? gateData = null;

        lockObj.EnterReadLock();
        try
        {
            gateData = openedGates.GetValueOrDefault(thisMapId);
        }
        finally
        {
            lockObj.ExitReadLock();
        }

        if (gateData != null)
        {
            chr.sendPacket(PacketCreator.environmentChange(gateData.Value.Key, gateData.Value.Value));
        }
    }

    public void giveEventPlayersStageReward(int thisStage)
    {
        List<int> list = getClearStageBonus(thisStage);     // will give bonus exp & mesos to everyone in the event
        giveEventPlayersExp(list[0]);
        giveEventPlayersMeso(list[1]);
    }

    public void linkToNextStage(int thisStage, string eventFamily, int thisMapId)
    {
        giveEventPlayersStageReward(thisStage);
        thisStage--;    //stages counts from ONE, scripts from ZERO
        IMap nextStage = getMapInstance(thisMapId);
        var portal = nextStage.getPortal("next00");
        portal?.setScriptName(eventFamily + thisStage);
    }

    public void linkPortalToScript(int thisStage, string portalName, string scriptName, int thisMapId)
    {
        giveEventPlayersStageReward(thisStage);
        // thisStage--;    //stages counts from ONE, scripts from ZERO
        IMap nextStage = getMapInstance(thisMapId);
        var portal = nextStage.getPortal(portalName);
        portal?.setScriptName(scriptName);
    }

    // registers a player status in an event
    public void gridInsert(IPlayer chr, int newStatus)
    {
        lockObj.EnterWriteLock();
        try
        {
            playerGrid.AddOrUpdate(chr.getId(), newStatus);
        }
        finally
        {
            lockObj.ExitWriteLock();
        }
    }

    // unregisters a player status in an event
    public void gridRemove(IPlayer chr)
    {
        lockObj.EnterWriteLock();
        try
        {
            playerGrid.Remove(chr.getId());
        }
        finally
        {
            lockObj.ExitWriteLock();
        }
    }

    // checks a player status
    public int gridCheck(IPlayer chr)
    {
        lockObj.EnterReadLock();
        try
        {
            return playerGrid.GetValueOrDefault(chr.getId(), -1);
        }
        finally
        {
            lockObj.ExitReadLock();
        }
    }

    public int gridSize()
    {
        lockObj.EnterReadLock();
        try
        {
            return playerGrid.Count;
        }
        finally
        {
            lockObj.ExitReadLock();
        }
    }

    public void gridClear()
    {
        lockObj.EnterWriteLock();
        try
        {
            playerGrid.Clear();
        }
        finally
        {
            lockObj.ExitWriteLock();
        }
    }

    public bool activatedAllReactorsOnMap(int mapId, int minReactorId, int maxReactorId)
    {
        return activatedAllReactorsOnMap(this.getMapInstance(mapId), minReactorId, maxReactorId);
    }

    public bool activatedAllReactorsOnMap(IMap map, int minReactorId, int maxReactorId)
    {
        if (map == null)
        {
            return true;
        }

        foreach (Reactor mr in map.getReactorsByIdRange(minReactorId, maxReactorId))
        {
            if (mr.getReactorType() != -1)
            {
                return false;
            }
        }

        return true;
    }
}

