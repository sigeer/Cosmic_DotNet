﻿/* @Author Ronan
 * @Author Vcoc
        Name: Steward
        Map(s): Foyer
        Info: Commands
        Script: commands.js
*/

var status;

var common_heading = "@";
var staff_heading = "!";

var levels = ["Common", "Donator", "JrGM", "GM", "SuperGM", "Developer", "Admin"];
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
            var sendStr = "There are all available commands for you:\r\n\r\n#b";
            for (var i = 0; i <= cm.getPlayer().gmLevel(); i++) {
                sendStr += "#L" + i + "#" + levels[i] + "#l\r\n";
            }

            cm.sendSimple(sendStr);
        } else if (status == 1) {
            var lvHead = (selection < 2) ? common_heading : staff_heading;

            if (selection > 6) {
                selection = 6;
            } else if (selection < 0) {
                selection = 0;
            }

            var levelData = commands[selection];

            var sendStr = "The following commands are available for #b" + levels[selection] + "#k:\r\n\r\n";
            for (var i = 0; i < levelData.size(); i++) {
                sendStr += "  #L" + i + "# " + lvHead + levelData.get(i).Name + " - " + levelData.get(i).Description;
                sendStr += "#l\r\n";
            }

            cm.sendPrev(sendStr);
        } else {
            cm.dispose();
        }
    }
}
