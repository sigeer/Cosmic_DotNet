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


using Application.Core.Game.Maps;
using Application.Core.scripting.Event;
using client.inventory;
using net.packet;
using net.server.services.task.channel;
using net.server.services.type;
using scripting.reactor;
using server.partyquest;
using tools;

namespace server.maps;


/// <summary>
/// 箱子之类的地图上可攻击的对象？
/// @author Lerk
/// @author Ronan
/// </summary>
public class Reactor : AbstractMapObject
{
    private int rid;
    private ReactorStats stats;
    private sbyte state;
    private byte evstate;
    private int delay;

    private string name;
    private bool alive;
    private bool shouldCollect;
    private bool attackHit;
    private ScheduledFuture? timeoutTask = null;
    private Action? delayedRespawnRun = null;
    private GuardianSpawnPoint? guardian = null;
    private byte facingDirection = 0;
    private object reactorLock = new object();
    private object hitLock = new object();

    public Reactor(ReactorStats stats, int rid)
    {
        this.evstate = 0;
        this.stats = stats;
        this.rid = rid;
        this.alive = true;
    }

    public void setShouldCollect(bool collect)
    {
        this.shouldCollect = collect;
    }

    public bool getShouldCollect()
    {
        return shouldCollect;
    }

    public void lockReactor()
    {
        Monitor.Enter(reactorLock);
    }

    public void unlockReactor()
    {
        Monitor.Exit(reactorLock);
    }

    public void hitLockReactor()
    {
        Monitor.Enter(hitLock);
        Monitor.Enter(reactorLock);
    }

    public void hitUnlockReactor()
    {
        Monitor.Exit(reactorLock);
        Monitor.Exit(hitLock);
    }

    public void setState(sbyte state)
    {
        this.state = state;
    }

    public sbyte getState()
    {
        return state;
    }

    public void setEventState(byte substate)
    {
        this.evstate = substate;
    }

    public byte getEventState()
    {
        return evstate;
    }

    public ReactorStats getStats()
    {
        return stats;
    }

    public int getId()
    {
        return rid;
    }

    public void setDelay(int delay)
    {
        this.delay = delay;
    }

    public int getDelay()
    {
        return delay;
    }

    public override MapObjectType getType()
    {
        return MapObjectType.REACTOR;
    }

    public int getReactorType()
    {
        return stats.getType(state);
    }

    public bool isRecentHitFromAttack()
    {
        return attackHit;
    }

    public ItemQuantity? getReactItem(byte index)
    {
        return stats.getReactItem(state, index);
    }

    public bool isAlive()
    {
        return alive;
    }

    public bool isActive()
    {
        return alive && stats.getType(state) != -1;
    }

    public void setAlive(bool alive)
    {
        this.alive = alive;
    }

    public override void sendDestroyData(IClient client)
    {
        client.sendPacket(makeDestroyData());
    }

    public Packet makeDestroyData()
    {
        return PacketCreator.destroyReactor(this);
    }

    public override void sendSpawnData(IClient client)
    {
        if (this.isAlive())
        {
            client.sendPacket(makeSpawnData());
        }
    }

    public Packet makeSpawnData()
    {
        return PacketCreator.spawnReactor(this);
    }

    public void resetReactorActions(int newState)
    {
        setState((sbyte)newState);
        cancelReactorTimeout();
        setShouldCollect(true);
        refreshReactorTimeout();

        if (MapModel != null)
        {
            MapModel.searchItemReactors(this);
        }
    }

    public void forceHitReactor(byte newState)
    {
        this.lockReactor();
        try
        {
            this.resetReactorActions(newState);
            MapModel.broadcastMessage(PacketCreator.triggerReactor(this, 0));
        }
        finally
        {
            this.unlockReactor();
        }
    }

    private void tryForceHitReactor(byte newState)
    {  // weak hit state signal, if already changed reactor state before timeout then drop this
        if (!Monitor.TryEnter(reactorLock))
        {
            return;
        }

        try
        {
            this.resetReactorActions(newState);
            MapModel.broadcastMessage(PacketCreator.triggerReactor(this, 0));
        }
        finally
        {
            Monitor.Exit(reactorLock);
        }
    }

    public void cancelReactorTimeout()
    {
        if (timeoutTask != null)
        {
            timeoutTask.cancel(false);
            timeoutTask = null;
        }
    }

    private void refreshReactorTimeout()
    {
        int timeOut = stats.getTimeout(state);
        if (timeOut > -1)
        {
            byte nextState = stats.getTimeoutState(state);

            timeoutTask = TimerManager.getInstance().schedule(() =>
            {
                timeoutTask = null;
                tryForceHitReactor(nextState);
            }, timeOut);
        }
    }

    public void delayedHitReactor(IClient c, long delay)
    {
        TimerManager.getInstance().schedule(() =>
        {
            hitReactor(c);
        }, delay);
    }

    public void hitReactor(IClient c)
    {
        hitReactor(false, 0, 0, 0, c);
    }

    public void hitReactor(bool wHit, int charPos, short stance, int skillid, IClient c)
    {
        try
        {
            if (!this.isActive())
            {
                return;
            }

            if (Monitor.TryEnter(hitLock))
            {
                this.lockReactor();
                try
                {
                    cancelReactorTimeout();
                    attackHit = wHit;

                    if (YamlConfig.config.server.USE_DEBUG)
                    {
                        c.OnlinedCharacter.dropMessage(5, "Hitted REACTOR " + this.getId() + " with POS " + charPos + " , STANCE " + stance + " , SkillID " + skillid + " , STATE " + state + " STATESIZE " + stats.getStateSize(state));
                    }
                    ReactorScriptManager.getInstance().onHit(c, this);

                    int reactorType = stats.getType(state);
                    if (reactorType < 999 && reactorType != -1)
                    {
                        //type 2 = only hit from right (kerning swamp plants), 00 is air left 02 is ground left
                        if (!(reactorType == 2 && (stance == 0 || stance == 2)))
                        {
                            //get next state
                            for (byte b = 0; b < stats.getStateSize(state); b++)
                            {
                                //YAY?
                                var activeSkills = stats.getActiveSkills(state, b);
                                if (activeSkills != null)
                                {
                                    if (!activeSkills.Contains(skillid))
                                    {
                                        continue;
                                    }
                                }

                                this.state = stats.getNextState(state, b);
                                sbyte nextState = stats.getNextState(state, b);
                                bool isInEndState = nextState < this.state;
                                if (isInEndState)
                                {
                                    //end of reactor
                                    if (reactorType < 100)
                                    {
                                        //reactor broken
                                        // delay > 0：延迟重生，delay < 0：不重生 delay = 0 无法销毁
                                        if (delay != 0)
                                        {
                                            MapModel.destroyReactor(getObjectId());
                                        }
                                        else
                                        {
                                            //trigger as normal
                                            MapModel.broadcastMessage(PacketCreator.triggerReactor(this, stance));
                                        }
                                    }
                                    else
                                    {
                                        //item-triggered on step
                                        MapModel.broadcastMessage(PacketCreator.triggerReactor(this, stance));
                                    }

                                    ReactorScriptManager.getInstance().act(c, this);
                                }
                                else
                                {
                                    //reactor not broken yet
                                    MapModel.broadcastMessage(PacketCreator.triggerReactor(this, stance));
                                    if (state == stats.getNextState(state, b))
                                    {
                                        //current state = next state, looping reactor
                                        ReactorScriptManager.getInstance().act(c, this);
                                    }

                                    setShouldCollect(true);     // refresh collectability on item drop-based reactors
                                    refreshReactorTimeout();
                                    if (stats.getType(state) == 100)
                                    {
                                        MapModel.searchItemReactors(this);
                                    }
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        state++;
                        MapModel.broadcastMessage(PacketCreator.triggerReactor(this, stance));
                        if (this.getId() != 9980000 && this.getId() != 9980001)
                        {
                            ReactorScriptManager.getInstance().act(c, this);
                        }

                        setShouldCollect(true);
                        refreshReactorTimeout();
                        if (stats.getType(state) == 100)
                        {
                            MapModel.searchItemReactors(this);
                        }
                    }
                }
                finally
                {
                    this.unlockReactor();
                    Monitor.Exit(hitLock);   // non-encapsulated unlock found thanks to MiLin
                }
            }
        }
        catch (Exception e)
        {
            Log.Logger.Error(e.ToString());
        }
    }

    /// <summary>
    /// 只要箱子能重生，就不会从地图上移除，只是看不见
    /// </summary>
    /// <returns></returns>
    public bool destroy()
    {
        if (Monitor.TryEnter(reactorLock))
        {
            try
            {
                bool alive = this.isAlive();
                // reactor neither alive nor in delayed respawn, remove map object allowed
                if (alive)
                {
                    this.setAlive(false);
                    this.cancelReactorTimeout();

                    if (this.getDelay() > 0)
                    {
                        this.delayedRespawn();
                    }
                }
                else
                {
                    return delayedRespawnRun == null;
                }
            }
            finally
            {
                Monitor.Exit(reactorLock);
            }
        }

        MapModel.broadcastMessage(PacketCreator.destroyReactor(this));
        return this.getDelay() < 0;
    }

    private void respawn()
    {
        this.lockReactor();
        try
        {
            this.resetReactorActions(0);
            this.setAlive(true);
        }
        finally
        {
            this.unlockReactor();
        }

        MapModel.broadcastMessage(this.makeSpawnData());
    }

    public void delayedRespawn()
    {
        delayedRespawnRun = () =>
        {
            delayedRespawnRun = null;
            respawn();
        };

        OverallService service = (OverallService)MapModel.getChannelServer().getServiceAccess(ChannelServices.OVERALL);
        service.registerOverallAction(MapModel.getId(), delayedRespawnRun, this.getDelay());
    }

    public bool forceDelayedRespawn()
    {
        Action? r = delayedRespawnRun;

        if (r != null)
        {
            OverallService service = (OverallService)MapModel.getChannelServer().getServiceAccess(ChannelServices.OVERALL);
            service.forceRunOverallAction(MapModel.getId(), r);
            return true;
        }
        else
        {
            return false;
        }
    }

    public Rectangle getArea()
    {
        return new Rectangle(getPosition().X + stats.getTL().X, getPosition().Y + stats.getTL().Y, stats.getBR().X - stats.getTL().X, stats.getBR().Y - stats.getTL().Y);
    }

    public string getName()
    {
        return name;
    }

    public void setName(string name)
    {
        this.name = name;
    }

    public GuardianSpawnPoint? getGuardian()
    {
        return guardian;
    }

    public void setGuardian(GuardianSpawnPoint? guardian)
    {
        this.guardian = guardian;
    }

    public void setFacingDirection(byte facingDirection)
    {
        this.facingDirection = facingDirection;
    }

    public byte getFacingDirection()
    {
        return facingDirection;
    }
}
