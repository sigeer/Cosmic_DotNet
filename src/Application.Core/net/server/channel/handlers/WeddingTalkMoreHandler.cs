/*
    This file is part of the HeavenMS MapleStory Server
    Copyleft (L) 2016 - 2019 RonanLana

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



using client;
using net.packet;
using tools;
using tools.packets;

namespace net.server.channel.handlers;

/**
 * @author Ronan
 */
public class WeddingTalkMoreHandler : AbstractPacketHandler
{

    public override void handlePacket(InPacket p, Client c)
    {
        var eim = c.getPlayer().getEventInstance();
        if (eim != null && !(c.getPlayer().getId() == eim.getIntProperty("groomId") || c.getPlayer().getId() == eim.getIntProperty("brideId")))
        {
            eim.gridInsert(c.getPlayer(), 1);
            c.getPlayer().dropMessage(5, "High Priest John: Your blessings have been added to their love. What a noble act for a lovely couple!");
        }

        c.sendPacket(WeddingPackets.OnWeddingProgress(true, 0, 0, 3));
        c.sendPacket(PacketCreator.enableActions());
    }
}