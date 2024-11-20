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
public class MobRequirement : AbstractQuestRequirement
{
    private static ILogger log = LogFactory.GetLogger("MobRequirement");
    Dictionary<int, int> mobs = new();
    private int questID;

    public MobRequirement(int questId, IRequirementMobAdapter adapter) : base(adapter)
    {
        questID = questId;
        mobs = adapter.GetData();
    }



    public override bool check(IPlayer chr, int? npcid)
    {
        QuestStatus status = chr.getQuest(Quest.getInstance(questID));
        foreach (int mobID in mobs.Keys)
        {
            var countReq = mobs.get(mobID);
            int progress;

            try
            {
                progress = int.Parse(status.getProgress(mobID));
            }
            catch (FormatException ex)
            {
                log.Warning(ex, "Mob: {MobId}, quest: {QuestId}, chrId: {CharacterId}, progress: {Progress}", mobID, questID, chr.getId(), status.getProgress(mobID));
                return false;
            }

            if (progress < countReq)
            {
                return false;
            }
        }
        return true;
    }

    public int getRequiredMobCount(int mobid)
    {
        return mobs.GetValueOrDefault(mobid);
    }
}
