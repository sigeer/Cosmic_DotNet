﻿/*
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

var status;

function start() {
    status = -1;
    action(1, 0, 0);
}

function action(mode, type, selection) {
    if (mode == -1) {
        cm.dispose();
    } else {
        if (mode == 0 && type > 0) {
            cm.dispose();
            return;
        }
        if (mode == 1) {
            status++;
        } else {
            status--;
        }

        if (status == 0) {
            var qs = cm.getQuestStatus(2162);

            if ((qs == 0 || qs == 1) && !cm.haveItem(4031839, 1)) {
                if (cm.canHold(4031839, 1)) {
                    cm.gainItem(4031839, 1);
                    cm.sendNext("你检索到了一个从垃圾桶中取出的皱巴巴的纸张。它的内容似乎很重要。");
                } else {
                    cm.sendNext("(你看到一个从垃圾桶中伸出的皱巴巴的纸张。内容似乎很重要，但由于你的背包已满，无法将其取出。)");
                }
            }

            cm.dispose();
        }
    }
}