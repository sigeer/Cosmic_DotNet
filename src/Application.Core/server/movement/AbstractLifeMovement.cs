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
using net.packet;

namespace server.movement;



public abstract class AbstractLifeMovement : LifeMovement
{
    private Point position;
    private int duration;
    private int newstate;
    private int type;

    public AbstractLifeMovement(int type, Point position, int duration, int newstate) : base()
    {
        this.type = type;
        this.position = position;
        this.duration = duration;
        this.newstate = newstate;
    }

    public int getType()
    {
        return this.type;
    }

    public int getDuration()
    {
        return duration;
    }

    public int getNewstate()
    {
        return newstate;
    }

    public Point getPosition()
    {
        return position;
    }

    public abstract void serialize(OutPacket p);
}
