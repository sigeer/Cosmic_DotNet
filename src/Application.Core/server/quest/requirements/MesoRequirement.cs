/*
    This file is part of the HeavenMS MapleStory Server
    Copyleft (L) 2016 - 2019 RonanLana

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

namespace server.quest.requirements;

/**
 * @author Ronan
 */
public class MesoRequirement : AbstractQuestRequirement
{
    private int meso = 0;

    public MesoRequirement(IRequirementDataAdapter adapter) : base(adapter)
    {
        meso = adapter.GetIntValue();
    }


    public override bool check(IPlayer chr, int? npcid)
    {
        if (chr.getMeso() >= meso)
        {
            return true;
        }
        else
        {
            chr.dropMessage(5, "You don't have enough mesos to complete this quest.");
            return false;
        }
    }
}
