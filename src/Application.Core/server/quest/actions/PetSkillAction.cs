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
using client;
using constants.inventory;

namespace server.quest.actions;

/**
 * @author Tyler (Twdtwd)
 */
public class PetSkillAction : AbstractQuestAction
{
    int flag;

    public PetSkillAction(IRewardDataAdapter action, Quest quest) : base(action, quest)
    {
        flag = action.GetIntValue();
    }

    public override bool check(IPlayer chr, int? extSelection)
    {
        QuestStatus status = chr.getQuest(Quest.getInstance(questID));
        if (!(status.getStatus() == QuestStatus.Status.NOT_STARTED && status.getForfeited() > 0))
        {
            return false;
        }

        return chr.getPet(0) != null;
    }

    public override void run(IPlayer chr, int? extSelection)
    {
        chr.getPet(0).setFlag((byte)ItemConstants.getFlagByInt(flag));
    }
}
