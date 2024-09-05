/*
    This file is part of the HeavenMS MapleStory NewServer
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



using net.packet;
using tools;
using tools.packets;

namespace net.server.channel.handlers;

/**
 * @author Ronan
 */
public class WeddingTalkHandler : AbstractPacketHandler
{

    public override void HandlePacket(InPacket p, IClient c)
    {
        byte action = p.readByte();
        if (action == 1)
        {
            var eim = c.OnlinedCharacter.getEventInstance();

            if (eim != null && !(c.OnlinedCharacter.getId() == eim.getIntProperty("groomId") || c.OnlinedCharacter.getId() == eim.getIntProperty("brideId")))
            {
                c.sendPacket(WeddingPackets.OnWeddingProgress(false, 0, 0, 2));
            }
            else
            {
                c.sendPacket(WeddingPackets.OnWeddingProgress(true, 0, 0, 3));
            }
        }
        else
        {
            c.sendPacket(WeddingPackets.OnWeddingProgress(true, 0, 0, 3));
        }

        c.sendPacket(PacketCreator.enableActions());
    }
}