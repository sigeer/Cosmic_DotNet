/*
This file is part of the OdinMS Maple Story NewServer
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
using tools;

namespace net.server.channel.handlers;

/**
 * @author kevintjuh93
 */
public class SnowballHandler : AbstractPacketHandler
{

    public override void HandlePacket(InPacket p, IClient c)
    {
        //D3 00 02 00 00 A5 01
        var chr = c.OnlinedCharacter;
        var map = chr.getMap();
        var snowball = map.getSnowball(chr.getTeam());
        var othersnowball = map.getSnowball(chr.getTeam() == 0 ? 1 : 0);
        int what = p.readByte();
        //slea.skip(4);

        if (snowball == null || othersnowball == null || snowball.getSnowmanHP() == 0)
        {
            return;
        }
        if ((currentServerTime() - chr.getLastSnowballAttack()) < 500)
        {
            return;
        }
        if (chr.getTeam() != (what % 2))
        {
            return;
        }

        chr.setLastSnowballAttack(currentServerTime());
        int damage = 0;
        if (what < 2 && othersnowball.getSnowmanHP() > 0)
        {
            damage = 10;
        }
        else if (what == 2 || what == 3)
        {
            if (Randomizer.nextDouble() < 0.03)
            {
                damage = 45;
            }
            else
            {
                damage = 15;
            }
        }

        if (what >= 0 && what <= 4)
        {
            snowball.hit(what, damage);
        }

    }
}
