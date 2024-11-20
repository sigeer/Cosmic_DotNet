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
public class IntervalRequirement : AbstractQuestRequirement
{
    private long interval = -1;
    private int questID;
    public IntervalRequirement(int questId, IRequirementDataAdapter adapter) : base(adapter)
    {
        questID = questId;
        interval = adapter.GetIntValue() * 60 * 1000;
    }

    public long getInterval()
    {
        return interval;
    }

    private static string getIntervalTimeLeft(IPlayer chr, IntervalRequirement r)
    {
        long futureTime = chr.getQuest(Quest.getInstance(r.questID)).getCompletionTime() + r.getInterval();
        var leftTime = DateTimeOffset.FromUnixTimeMilliseconds(futureTime) - DateTimeOffset.Now;

        List<string> messages = new List<string>();

        if (leftTime.Hours > 0)
            messages.Add($"{leftTime.Hours} hours");
        if (leftTime.Minutes > 0)
            messages.Add($"{leftTime.Minutes} minutes");
        if (leftTime.Seconds > 0)
            messages.Add($"{leftTime.Seconds} seconds");

        return string.Join(", ", messages);
    }

    public override bool check(IPlayer chr, int? npcid)
    {
        bool check = !chr.getQuest(Quest.getInstance(questID)).getStatus().Equals(QuestStatus.Status.COMPLETED);
        bool check2 = chr.getQuest(Quest.getInstance(questID)).getCompletionTime() <= DateTimeOffset.Now.ToUnixTimeMilliseconds() - interval;

        if (check || check2)
        {
            return true;
        }
        else
        {
            chr.message("This quest will become available again in approximately " + getIntervalTimeLeft(chr, this) + ".");
            return false;
        }
    }
}
