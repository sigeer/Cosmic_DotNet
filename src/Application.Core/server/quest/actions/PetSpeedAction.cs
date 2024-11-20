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


using Application.Core.Game.Items;
using Application.Core.Game.QuestDomain.RewardAdapter;

namespace server.quest.actions;

/**
 * @author Ronan
 */
public class PetSpeedAction : AbstractQuestAction
{
    public PetSpeedAction(IRewardDataAdapter action, Quest quest) : base(action, quest)
    {
    }

    public override void run(IPlayer chr, int? extSelection)
    {
        var c = chr.getClient();

        var pet = chr.getPet(0);   // assuming here only the pet leader will gain owner speed
        if (pet == null)
        {
            return;
        }

        c.lockClient();
        try
        {
            pet.addPetAttribute(chr, PetAttribute.OWNER_SPEED);
        }
        finally
        {
            c.unlockClient();
        }

    }
}
