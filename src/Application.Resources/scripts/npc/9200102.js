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
/* Dr. Bosch
	Ludibrium Random/VIP Eye Color Change.
 */
var status = 0;
var beauty = 0;
var regprice = 1000000;
var vipprice = 1000000;
var colors = Array();

function pushIfItemExists(array, itemid) {
    if ((itemid = cm.getCosmeticItem(itemid)) != -1 && !cm.isCosmeticEquipped(itemid)) {
        array.push(itemid);
    }
}

function pushIfItemsExists(array, itemidList) {
    for (var i = 0; i < itemidList.length; i++) {
        var itemid = itemidList[i];

        if ((itemid = cm.getCosmeticItem(itemid)) != -1 && !cm.isCosmeticEquipped(itemid)) {
            array.push(itemid);
        }
    }
}

function start() {
    status = -1;
    action(1, 0, 0);
}

function action(mode, type, selection) {
    if (mode < 1) {  // disposing issue with stylishs found thanks to Vcoc
        cm.dispose();
    } else {
        if (mode == 1) {
            status++;
        } else {
            status--;
        }
        if (status == 0) {
            cm.sendSimple("“嗯...嗨，我是博士博世，我是卢迪布里姆整形外科诊所的美瞳专家。我相信你的眼睛是你身体中最重要的特征，通过使用#b#t5152012##k或者#b#t5152015##k，我可以为你开具适合的美瞳。现在，你想要使用哪种呢？\r\n#L1#美瞳：#i5152012##t5152012##l\r\n#L2#美瞳：#i5152015##t5152015##l\r\n#L3#一次性美瞳：#i5152105#（任何颜色）#l");
        } else if (status == 1) {
            if (selection == 1) {
                beauty = 1;
                if (cm.getPlayer().getGender() == 0) {
                    var current = cm.getPlayer().getFace() % 100 + 20000;
                }
                if (cm.getPlayer().getGender() == 1) {
                    var current = cm.getPlayer().getFace() % 100 + 21000;
                }
                colors = Array();
                pushIfItemsExists(colors, [current + 200, current + 300, current + 400, current + 500, current + 700]);
                cm.sendYesNo("如果你使用普通优惠券，你将获得一副随机的化妆隐形眼镜。你打算使用#b#t5152012##k，真的改变你的眼睛吗？");
            } else if (selection == 2) {
                beauty = 2;
                if (cm.getPlayer().getGender() == 0) {
                    var current = cm.getPlayer().getFace() % 100 + 20000;
                }
                if (cm.getPlayer().getGender() == 1) {
                    var current = cm.getPlayer().getFace() % 100 + 21000;
                }
                colors = Array();
                pushIfItemsExists(colors, [current + 200, current + 300, current + 400, current + 500, current + 700]);
                cm.sendStyle("用我们新的计算机程序，你可以提前看到治疗后的自己。你想戴什么样的美瞳？请选择您喜欢的风格。", colors);
            } else if (selection == 3) {
                beauty = 3;
                if (cm.getPlayer().getGender() == 0) {
                    var current = cm.getPlayer().getFace()
                        % 100 + 20000;
                }
                if (cm.getPlayer().getGender() == 1) {
                    var current = cm.getPlayer().getFace()
                        % 100 + 21000;
                }

                colors = Array();
                for (var i = 0; i < 8; i++) {
                    if (cm.haveItem(5152100 + i)) {
                        pushIfItemExists(colors, current + 100 * i);
                    }
                }

                if (colors.length == 0) {
                    cm.sendOk("你没有任何一次性化妆镜片可供使用。");
                    cm.dispose();
                    return;
                }

                cm.sendStyle("你想戴什么样的眼镜？请选择您喜欢的风格。", colors);
            }
        } else if (status == 2) {
            if (beauty == 1) {
                if (cm.haveItem(5152012) == true) {
                    cm.gainItem(5152012, -1);
                    cm.setFace(colors[Math.floor(Math.random() * colors.length)]);
                    cm.sendOk("享受你的新款和升级版的隐形眼镜吧！");
                    cm.dispose();
                } else {
                    cm.sendOk("对不起，但我觉得你现在没有我们的化妆镜优惠券。没有优惠券，恐怕我不能为你做。");
                    cm.dispose();
                }
            } else if (beauty == 2) {
                if (cm.haveItem(5152015) == true) {
                    cm.gainItem(5152015, -1);
                    cm.setFace(colors[selection]);
                    cm.sendOk("享受你的新款和升级版的隐形眼镜吧！");
                    cm.dispose();
                } else {
                    cm.sendOk("对不起，但我不认为你现在带着我们的化妆镜片优惠券。没有优惠券，恐怕我不能为你做这件事。");
                    cm.dispose();
                }
            } else if (beauty == 3) {
                var color = (colors[selection] / 100) % 10 | 0;

                if (cm.haveItem(5152100 + color)) {
                    cm.gainItem(5152100 + color, -1);
                    cm.setFace(colors[selection]);
                    cm.sendOk("享受你的新款和升级版的隐形眼镜吧！");
                } else {
                    cm.sendOk("对不起，但我觉得你现在没有我们的化妆镜片优惠券。没有优惠券，恐怕我不能为你做。");
                }
            } else if (beauty == 0) {
                if (selection == 0 && cm.getMeso() >= regprice) {
                    cm.gainMeso(-regprice);
                    cm.gainItem(5152012, 1);
                    cm.sendOk("享受！");
                    cm.dispose();
                } else if (selection == 1 && cm.getMeso() >= vipprice) {
                    cm.gainMeso(-vipprice);
                    cm.gainItem(5152015, 1);
                    cm.sendOk("享受！");
                    cm.dispose();
                } else {
                    cm.sendOk("你没有足够的金币来购买优惠券！");
                }
            }
        }
    }
}