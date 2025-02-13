/* @Author Ronan
 * @Author Vcoc
        Name: Steward
        Map(s): Foyer
        Info: Commands
        Script: commands.js
*/

var status;

var staff_heading = "!";

// var levels = ["Common", "Donator", "JrGM", "GM", "SuperGM", "Developer", "Admin"];
var levels = ["通用", "贡献者", "小GM", "GM", "大GM", "开发者", "超级管理员"];
var commands;

function writeHeavenMSCommands() {
    commands = CommandExecutor.getInstance().getGmCommands();
}

function start() {
    status = -1;
    writeHeavenMSCommands();
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
            var sendStr = "可使用的指令：\r\n\r\n#b";
            for (var i = 0; i <= cm.getPlayer().gmLevel(); i++) {
                sendStr += "#L" + i + "#" + levels[i] + "#l\r\n";
            }

            cm.sendSimple(sendStr);
        } else if (status == 1) {
            if (selection > 6) {
                selection = 6;
            } else if (selection < 0) {
                selection = 0;
            }

            var levelData = commands[selection];

            var sendStr = "该选项可用指令 #b" + levels[selection] + "#k:\r\n\r\n";
            for (var i = 0; i < levelData.size(); i++) {
                sendStr += "  #L" + i + "# " + staff_heading + levelData.get(i).Name + " - " + levelData.get(i).Description;
                sendStr += "#l\r\n";
            }

            cm.sendPrev(sendStr);
        } else {
            cm.dispose();
        }
    }
}