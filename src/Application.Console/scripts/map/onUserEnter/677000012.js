﻿function start(ms) {
    var pos = new Point(842, 0);
    var mobId = 9400633;
    var mobName = "Astaroth";

    var player = ms.getPlayer();
    var map = player.getMap();

    if (map.getMonsterById(mobId) != null) {
        return;
    }
    map.spawnMonsterOnGroundBelow(LifeFactory.getMonster(mobId), pos);
    player.message(mobName + " has appeared!");
}