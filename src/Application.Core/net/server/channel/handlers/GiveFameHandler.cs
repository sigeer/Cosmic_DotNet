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


using client.autoban;
using net.packet;
using tools;
using static Application.Core.Game.Players.Player;

namespace net.server.channel.handlers;

public class GiveFameHandler : AbstractPacketHandler
{

    public override void HandlePacket(InPacket p, IClient c)
    {
        var target = c.OnlinedCharacter.getMap().getMapObject(p.readInt()) as IPlayer;
        int mode = p.readByte();
        int famechange = 2 * mode - 1;
        var player = c.OnlinedCharacter;
        if (target == null || target.getId() == player.getId() || player.getLevel() < 15)
        {
            return;
        }
        else if (famechange != 1 && famechange != -1)
        {
            AutobanFactory.PACKET_EDIT.alert(c.OnlinedCharacter, c.OnlinedCharacter.getName() + " tried to packet edit fame.");
            log.Warning("Chr {CharacterName} tried to fame hack with famechange {FameChange}", c.OnlinedCharacter.getName(), famechange);
            c.disconnect(true, false);
            return;
        }

        var status = player.canGiveFame(target);
        if (status == FameStatus.OK)
        {
            if (target.gainFame(famechange, player, mode))
            {
                if (!player.isGM())
                {
                    player.hasGivenFame(target);
                }
            }
            else
            {
                player.message("Could not process the request, since this character currently has the minimum/maximum level of fame.");
            }
        }
        else
        {
            c.sendPacket(PacketCreator.giveFameErrorResponse(status == FameStatus.NOT_TODAY ? 3 : 4));
        }
    }
}