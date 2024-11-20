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


using Application.Core.Game.QuestDomain.RewardAdapter;
using Application.Core.Game.Skills;
using client;

namespace server.quest.actions;






/**
 * @author Tyler (Twdtwd)
 */
public class SkillAction : AbstractQuestAction
{
    Dictionary<int, SkillData> skillData = new();

    public SkillAction(IRewardSkillAdapter adapter, Quest quest) : base(adapter, quest)
    {
        skillData = adapter.GetData();

    }

    public override void run(IPlayer chr, int? extSelection)
    {
        foreach (SkillData skill in skillData.Values)
        {
            var skillObject = SkillFactory.getSkill(skill.getId());
            if (skillObject == null)
            {
                continue;
            }

            bool shouldLearn = skill.jobsContains(chr.getJobId()) || skillObject.isBeginnerSkill();

            sbyte skillLevel = (sbyte)Math.Max(skill.getLevel(), chr.getSkillLevel(skillObject));
            int masterLevel = Math.Max(skill.getMasterLevel(), chr.getMasterLevel(skillObject));
            if (shouldLearn)
            {
                chr.changeSkillLevel(skillObject, skillLevel, masterLevel, -1);
            }

        }
    }
}