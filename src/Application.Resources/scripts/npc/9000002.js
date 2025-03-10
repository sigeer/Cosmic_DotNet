﻿/*
    This file is part of the OdinMS Maple Story Server
    Copyright (C) 2008 Patrick Huy <patrick.huy@frz.cc> 
                       Matthias Butz <matze@odinms.de>
                       Jan Christian Meyer <vimes@odinms.de>

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License version 3
    as published by the Free Software Foundation. You may not use, modify
    or distribute this program under any other version of the
    GNU Affero General Public License.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
var status = 0;

function start() {
    status = -1;
    action(1, 0, 0);
}

function action(mode, type, selection) {
    if (mode == -1) {
        cm.dispose();
    } else if (mode == 0) {
        cm.dispose();
    } else {
        if (mode == 1) {
            status++;
        } else {
            status--;
        }

        if (status == 0) {
            cm.sendNext("轰隆隆隆！！你已经从#b活动#k中赢得了游戏。恭喜你走到了这一步！");
        } else if (status == 1) {
            cm.sendNext("你将获得#b秘密卷轴#k作为胜利奖励。卷轴上写有古代文字的秘密信息。");
        } else if (status == 2) {
            cm.sendNext("“秘密卷轴可以由鲁德斯里姆的#r春姬#k或#r吉尼#k解读。带上它，一定会发生好事。”");
        } else if (status == 3) {
            if (cm.canHold(4031019)) {
                cm.gainItem(4031019);
                cm.warp(cm.getPlayer().getSavedLocation("EVENT"));
                cm.dispose();
            } else {
                cm.sendNext("我认为你的杂项窗口已经满了。请腾出空间，然后和我交谈。");
            }
        } else if (status == 4) {
            cm.dispose();
        }
    }
}  