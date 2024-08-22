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
namespace net.encryption;

public class MapleCustomEncryption
{
    private static byte rollLeft(byte inValue, int count)
    {
        int tmp = inValue << (count % 8);
        return (byte)((tmp & 0xFF) | (tmp >> 8));
    }

    private static byte rollRight(byte inValue, int count)
    {
        int tmp = (int)((uint)(inValue << 8) >> (count % 8));

        return (byte)((tmp & 0xFF) | (int)((uint)tmp >> 8));
    }

    public static byte[] encryptData(byte[] data)
    {
        for (int j = 0; j < 6; j++)
        {
            byte remember = 0;
            byte dataLength = (byte)(data.Length & 0xFF);
            if (j % 2 == 0)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    byte cur = data[i];
                    cur = rollLeft(cur, 3);
                    cur += dataLength;
                    cur ^= remember;
                    remember = cur;
                    cur = rollRight(cur, dataLength & 0xFF);
                    cur = ((byte)((~cur) & 0xFF));
                    cur += 0x48;
                    dataLength--;
                    data[i] = cur;
                }
            }
            else
            {
                for (int i = data.Length - 1; i >= 0; i--)
                {
                    byte cur = data[i];
                    cur = rollLeft(cur, 4);
                    cur += dataLength;
                    cur ^= remember;
                    remember = cur;
                    cur ^= 0x13;
                    cur = rollRight(cur, 3);
                    dataLength--;
                    data[i] = cur;
                }
            }
        }
        return data;
    }

    public static byte[] decryptData(byte[] data)
    {
        for (int j = 1; j <= 6; j++)
        {
            byte remember = 0;
            byte dataLength = (byte)(data.Length & 0xFF);
            byte nextRemember;
            if (j % 2 == 0)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    byte cur = data[i];
                    cur -= 0x48;
                    cur = ((byte)((~cur) & 0xFF));
                    cur = rollLeft(cur, dataLength & 0xFF);
                    nextRemember = cur;
                    cur ^= remember;
                    remember = nextRemember;
                    cur -= dataLength;
                    cur = rollRight(cur, 3);
                    data[i] = cur;
                    dataLength--;
                }
            }
            else
            {
                for (int i = data.Length - 1; i >= 0; i--)
                {
                    byte cur = data[i];
                    cur = rollLeft(cur, 3);
                    cur ^= 0x13;
                    nextRemember = cur;
                    cur ^= remember;
                    remember = nextRemember;
                    cur -= dataLength;
                    cur = rollRight(cur, 4);
                    data[i] = cur;
                    dataLength--;
                }
            }
        }
        return data;
    }
}
