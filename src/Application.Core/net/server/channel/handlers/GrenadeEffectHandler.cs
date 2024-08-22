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
using constants.skills;
using net.packet;
using tools;

namespace net.server.channel.handlers;



/*
 * @author GabrielSin
 */
public class GrenadeEffectHandler : AbstractPacketHandler
{
    public override void handlePacket(InPacket p, Client c)
    {
        Character chr = c.getPlayer();
        Point position = new Point(p.readInt(), p.readInt());
        int keyDown = p.readInt();
        int skillId = p.readInt();

        switch (skillId)
        {
            case NightWalker.POISON_BOMB:
            case Gunslinger.GRENADE:
                int skillLevel = chr.getSkillLevel(skillId);
                if (skillLevel > 0)
                {
                    chr.getMap().broadcastMessage(chr, PacketCreator.throwGrenade(chr.getId(), position, keyDown, skillId, skillLevel), position);
                }
                break;
            default:
                log.Warning("The skill id: {SkillId} is not coded in {1}", skillId, GetType().Name);
                break;
        }
    }

}