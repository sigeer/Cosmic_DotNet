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


using Application.Core.model;
using client.inventory;
using constants.game;
using Microsoft.EntityFrameworkCore;
using server;
using server.movement;
using tools;

namespace Application.Core.Game.Items;

/**
 * @author Matze
 */
public class Pet : Item
{
    private int uniqueid;
    private int Fh;
    private Point pos;
    private int stance;
    private int petAttribute = 0;

    public string Name { get; set; } = null!;
    public int Fullness { get; set; } = MaxFullness;
    public int Tameness { get; set; }
    public byte Level { get; set; } = 1;
    public bool Summoned { get; set; }

    public const int MaxFullness = 100;
    public const int MaxTameness = 30000;
    public const int MaxLevel = 30;

    public Pet(int id, short position, int uniqueid) : base(id, position, 1)
    {
        log = LogFactory.GetLogger(LogType.Pet);
        this.uniqueid = uniqueid;
        pos = new Point(0, 0);
    }

    public void saveToDb()
    {
        try
        {
            using var dbContext = new DBContext();
            dbContext.Pets.Where(x => x.Petid == getUniqueId())
                .ExecuteUpdate(x =>
                    x.SetProperty(y => y.Flag, getPetAttribute())
                    .SetProperty(y => y.Name, Name)
                    .SetProperty(y => y.Level, Level)
                    .SetProperty(y => y.Closeness, Tameness)
                    .SetProperty(y => y.Fullness, Fullness)
                    .SetProperty(y => y.Summoned, Summoned));
        }
        catch (Exception e)
        {
            log.Error(e.ToString());
        }
    }

    public string getName()
    {
        return Name;
    }

    public void setName(string name)
    {
        Name = name;
    }

    public int getUniqueId()
    {
        return uniqueid;
    }

    public void setUniqueId(int id)
    {
        uniqueid = id;
    }

    public int getTameness()
    {
        return Tameness;
    }

    public void setTameness(int tameness)
    {
        Tameness = tameness;
    }

    public byte getLevel()
    {
        return Level;
    }

    public void gainTamenessFullness(IPlayer owner, int incTameness, int incFullness, int type)
    {
        gainTamenessFullness(owner, incTameness, incFullness, type, false);
    }

    public void gainTamenessFullness(IPlayer owner, int incTameness, int incFullness, int type, bool forceEnjoy)
    {
        sbyte slot = owner.getPetIndex(this);
        bool enjoyed;

        //will NOT increase pet's tameness if tried to feed pet with 100% fullness
        // unless forceEnjoy == true (cash shop)
        if (Fullness < MaxFullness || incFullness == 0 || forceEnjoy)
        {
            //incFullness == 0: command given
            int newFullness = Fullness + incFullness;
            if (newFullness > MaxFullness)
            {
                newFullness = MaxFullness;
            }
            Fullness = newFullness;

            if (incTameness > 0 && Tameness < MaxTameness)
            {
                int newTameness = Tameness + incTameness;
                if (newTameness > MaxTameness)
                {
                    newTameness = MaxTameness;
                }

                Tameness = newTameness;
                while (newTameness >= ExpTable.getTamenessNeededForLevel(Level))
                {
                    Level += 1;
                    owner.sendPacket(PacketCreator.showOwnPetLevelUp(slot));
                    owner.getMap().broadcastMessage(PacketCreator.showPetLevelUp(owner, slot));
                }
            }

            enjoyed = true;
        }
        else
        {
            int newTameness = Tameness - 1;
            if (newTameness < 0)
            {
                newTameness = 0;
            }

            Tameness = newTameness;
            if (Level > 1 && newTameness < ExpTable.getTamenessNeededForLevel(Level - 1))
            {
                Level -= 1;
            }

            enjoyed = false;
        }

        owner.getMap().broadcastMessage(PacketCreator.petFoodResponse(owner.getId(), slot, enjoyed, false));
        saveToDb();

        var petz = owner.getInventory(InventoryType.CASH).getItem(getPosition());
        if (petz != null)
        {
            owner.forceUpdateItem(petz);
        }
    }

    public void setLevel(byte level)
    {
        Level = level;
    }

    public int getFullness()
    {
        return Fullness;
    }

    public void setFullness(int fullness)
    {
        Fullness = fullness;
    }

    public int getFh()
    {
        return Fh;
    }

    public void setFh(int Fh)
    {
        this.Fh = Fh;
    }

    public Point getPos()
    {
        return pos;
    }

    public void setPos(Point pos)
    {
        this.pos = pos;
    }

    public int getStance()
    {
        return stance;
    }

    public void setStance(int stance)
    {
        this.stance = stance;
    }

    public bool isSummoned()
    {
        return Summoned;
    }

    public void setSummoned(bool yes)
    {
        Summoned = yes;
    }

    public int getPetAttribute()
    {
        return petAttribute;
    }

    public void setPetAttribute(int flag)
    {
        petAttribute = flag;
    }

    public void addPetAttribute(IPlayer owner, PetAttribute flag)
    {
        petAttribute |= (int)flag;
        saveToDb();

        Item? petz = owner.getInventory(InventoryType.CASH).getItem(getPosition());
        if (petz != null)
        {
            owner.forceUpdateItem(petz);
        }
    }

    public void removePetAttribute(IPlayer owner, PetAttribute flag)
    {
        petAttribute &= (int)(0xFFFFFFFF ^ (int)flag);
        saveToDb();

        Item? petz = owner.getInventory(InventoryType.CASH).getItem(getPosition());
        if (petz != null)
        {
            owner.forceUpdateItem(petz);
        }
    }

    public PetCanConsumePair canConsume(int itemId)
    {
        return ItemInformationProvider.getInstance().canPetConsume(getItemId(), itemId);
    }

    public void updatePosition(List<LifeMovementFragment> movement)
    {
        foreach (LifeMovementFragment move in movement)
        {
            if (move is LifeMovement)
            {
                if (move is AbsoluteLifeMovement)
                {
                    setPos(move.getPosition());
                }
                setStance(((LifeMovement)move).getNewstate());
            }
        }
    }
}

public enum PetAttribute
{
    OWNER_SPEED = 0x01
}