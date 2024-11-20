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


using Application.Core.Game.QuestDomain.RequirementAdapter;
using client;

namespace server.quest.requirements;




/**
 * @author Tyler (Twdtwd)
 */
public class JobRequirement : AbstractQuestRequirement
{
    List<int> jobs = new();

    public JobRequirement(IRequirementJobAdapter adapter) : base(adapter)
    {
        jobs = adapter.GetData();
    }


    public override bool check(IPlayer chr, int? npcid)
    {
        foreach (int job in jobs)
        {
            if (chr.getJob().Equals(JobUtils.getById(job)) || chr.isGM())
            {
                return true;
            }
        }
        return false;
    }
}
