/*
	This file is part of the MapleSolaxia Maple Story Server

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
public class QuestRequirement : AbstractQuestRequirement
{
    Dictionary<int, int> quests = new();

    public QuestRequirement(IRequirementQuestAdapter adapter) : base(adapter)
    {
        quests = adapter.GetData();
    }

    public override bool check(IPlayer chr, int? npcid)
    {
        foreach (int questID in quests.Keys)
        {
            var stateReq = quests.get(questID);
            QuestStatus qs = chr.getQuest(Quest.getInstance(questID));

            if (qs == null && stateReq == (int)QuestStatus.Status.NOT_STARTED)
            {
                continue;
            }

            if (qs == null || (int)qs.getStatus() != stateReq)
            {
                return false;
            }

        }
        return true;
    }
}
