﻿/*
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
var status = 0;
var zones = 0;
var names = Array("沉睡森林1", "沉睡森林2", "沉睡森林3");
var maps = Array(105040310, 105040312, 105040314);
var selectedMap = -1;

function start() {
    cm.sendNext("你感觉到一股神秘的力量笼罩着这座雕像。");
    if (cm.isQuestStarted(2054) || cm.isQuestCompleted(2054)) {
        zones = 3;
    } else if (cm.isQuestStarted(2053) || cm.isQuestCompleted(2053)) {
        zones = 2;
    } else if (cm.isQuestStarted(2052) || cm.isQuestCompleted(2052)) {
        zones = 1;
    } else {
        zones = 0;
    }
}

function action(mode, type, selection) {
    if (mode == -1) {
        cm.dispose();
    } else {
        if (status >= 2 && mode == 0) {
            cm.sendOk("好的，下次见。");
            cm.dispose();
            return;
        }
        if (mode == 1) {
            status++;
        } else {
            status--;
        }
        if (status == 1) {
            if (zones == 0) {
                cm.dispose();
            } else {
                var selStr = "它的力量让你能够深入森林深处。";
                for (var i = 0; i < zones; i++) {
                    selStr += "\r\n#L" + i + "#" + names[i] + "#l";
                }
                cm.sendSimple(selStr);
            }
        } else if (status == 2) {
            cm.warp(maps[selection], 0);
            cm.dispose();
        }
    }
}	