﻿function start(ms) {
    var pos = new Point(201, 80);
    var mobId = 9400609;
    var mobName = "Andras";

    var player = ms.getPlayer();
    var map = player.getMap();

    if (map.getMonsterById(mobId) != null) {
        return;
    }
    map.spawnMonsterOnGroundBelow(LifeFactory.getMonster(mobId), pos);
    player.message(mobName + " has appeared!");
}