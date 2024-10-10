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


using Application.Core.Game.Commands;
using client.autoban;
using net.packet;
using server;
using tools;

namespace net.server.channel.handlers;

public class GeneralChatHandler : AbstractPacketHandler
{
    public override void HandlePacket(InPacket p, IClient c)
    {
        string s = p.readString();
        var chr = c.OnlinedCharacter;
        if (chr.getAutobanManager().getLastSpam(7) + 200 > currentServerTime())
        {
            c.sendPacket(PacketCreator.enableActions());
            return;
        }
        if (s.Length > sbyte.MaxValue && !chr.isGM())
        {
            AutobanFactory.PACKET_EDIT.alert(c.OnlinedCharacter, c.OnlinedCharacter.getName() + " tried to packet edit in General Chat.");
            log.Warning("Chr {CharacterName} tried to send text with length of {StringLength}", c.OnlinedCharacter.getName(), s.Length);
            c.disconnect(true, false);
            return;
        }
        char heading = s.ElementAt(0);
        if (CommandExecutor.isCommand(c, s))
        {
            CommandExecutor.getInstance().handle(c, s);
        }
        else if (heading != '/')
        {
            int show = p.readByte();
            if (chr.getMap().isMuted() && !chr.isGM())
            {
                chr.dropMessage(5, "The map you are in is currently muted. Please try again later.");
                return;
            }

            if (!chr.isHidden())
            {
                chr.getMap().broadcastMessage(PacketCreator.getChatText(chr.getId(), s, chr.getWhiteChat(), show));
                ChatLogger.log(c, "General", s);
            }
            else
            {
                chr.getMap().broadcastGMMessage(PacketCreator.getChatText(chr.getId(), s, chr.getWhiteChat(), show));
                ChatLogger.log(c, "GM General", s);
            }

            chr.getAutobanManager().spam(7);
        }
    }
}