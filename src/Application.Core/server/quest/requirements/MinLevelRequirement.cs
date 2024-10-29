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

namespace server.quest.requirements;

/**
 * @author Tyler (Twdtwd)
 */
public class MinLevelRequirement : AbstractQuestRequirement
{
    private int minLevel;


    public MinLevelRequirement(Quest quest, Data data) : base(QuestRequirementType.MIN_LEVEL)
    {
        processData(data);
    }


    public override void processData(Data data)
    {
        minLevel = DataTool.getInt(data);
    }


    public override bool check(IPlayer chr, int? npcid)
    {
        return chr.getLevel() >= minLevel;
    }
}
