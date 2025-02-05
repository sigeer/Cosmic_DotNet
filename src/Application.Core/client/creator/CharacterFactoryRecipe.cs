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


using Application.Core.Game.Skills;
using client.inventory;

namespace client.creator;


/**
 * @author RonanLana
 */
public class CharacterFactoryRecipe
{
    private Job job;
    private int level;
    private int map;
    private int top;
    private int bottom;
    private int shoes;
    private int weapon;
    private int str = 4, dex = 4, int_ = 4, luk = 4;
    private int maxHp = 50, maxMp = 5;
    private int ap = 0, sp = 0;
    private int meso = 0;
    private List<KeyValuePair<Skill, int>> skills = new();

    private List<ItemInventoryType> itemsWithType = new();
    private Dictionary<InventoryType, AtomicInteger> runningTypePosition = new();

    public CharacterFactoryRecipe(Job job, int level, int map, int top, int bottom, int shoes, int weapon)
    {
        this.job = job;
        this.level = level;
        this.map = map;
        this.top = top;
        this.bottom = bottom;
        this.shoes = shoes;
        this.weapon = weapon;

        if (!YamlConfig.config.server.USE_STARTING_AP_4)
        {
            if (YamlConfig.config.server.USE_AUTOASSIGN_STARTERS_AP)
            {
                str = 12;
                dex = 5;
            }
            else
            {
                ap = 9;
            }
        }
    }

    public void setStr(int v)
    {
        str = v;
    }

    public void setDex(int v)
    {
        dex = v;
    }

    public void setInt(int v)
    {
        int_ = v;
    }

    public void setLuk(int v)
    {
        luk = v;
    }

    public void setMaxHp(int v)
    {
        maxHp = v;
    }

    public void setMaxMp(int v)
    {
        maxMp = v;
    }

    public void setRemainingAp(int v)
    {
        ap = v;
    }

    public void setRemainingSp(int v)
    {
        sp = v;
    }

    public void setMeso(int v)
    {
        meso = v;
    }

    public void addStartingSkillLevel(Skill skill, int level)
    {
        skills.Add(new(skill, level));
    }

    public void addStartingEquipment(Item eqpItem)
    {
        itemsWithType.Add(new(eqpItem, InventoryType.EQUIP));
    }

    public void addStartingItem(int itemid, int quantity, InventoryType itemType)
    {
        var p = runningTypePosition.GetValueOrDefault(itemType);
        if (p == null)
        {
            p = new AtomicInteger(0);
            runningTypePosition.AddOrUpdate(itemType, p);
        }

        itemsWithType.Add(new(new Item(itemid, (short)p.getAndIncrement(), (short)quantity), itemType));
    }

    public Job getJob()
    {
        return job;
    }

    public int getLevel()
    {
        return level;
    }

    public int getMap()
    {
        return map;
    }

    public int getTop()
    {
        return top;
    }

    public int getBottom()
    {
        return bottom;
    }

    public int getShoes()
    {
        return shoes;
    }

    public int getWeapon()
    {
        return weapon;
    }

    public int getStr()
    {
        return str;
    }

    public int getDex()
    {
        return dex;
    }

    public int getInt()
    {
        return int_;
    }

    public int getLuk()
    {
        return luk;
    }

    public int getMaxHp()
    {
        return maxHp;
    }

    public int getMaxMp()
    {
        return maxMp;
    }

    public int getRemainingAp()
    {
        return ap;
    }

    public int getRemainingSp()
    {
        return sp;
    }

    public int getMeso()
    {
        return meso;
    }

    public List<KeyValuePair<Skill, int>> getStartingSkillLevel()
    {
        return skills;
    }

    public List<ItemInventoryType> getStartingItems()
    {
        return itemsWithType;
    }
}
