using Application.Core.Game.Items;
using client.inventory;
using client.inventory.manipulator;
using constants.inventory;
using Microsoft.EntityFrameworkCore;
using Serilog;
using server;

namespace Application.Core.Managers
{
    public class ItemManager
    {
        #region pet
        static ILogger petLog = LogFactory.GetLogger(LogType.Pet);

        public static Pet? loadFromDb(int itemid, short position, int petid)
        {
            Pet ret = new Pet(itemid, position, petid);
            try
            {
                // Get the pet details...
                using var dbContext = new DBContext();
                var dbModel = dbContext.Pets.FirstOrDefault(x => x.Petid == petid);
                GlobalTools.Mapper.Map(dbModel, ret);
                return ret;
            }
            catch (Exception e)
            {
                petLog.Error(e.ToString());
                return null;
            }
        }

        public static int CreatePet(int itemid) => CreatePet(itemid, 1, 0, 100);
        public static int CreatePet(int itemid, byte level, int tameness, int fullness)
        {
            try
            {
                using var dbContext = new DBContext();
                var dbModel = new PetEntity
                {
                    Petid = CashIdGenerator.generateCashId(),
                    Name = ItemInformationProvider.getInstance().getName(itemid),
                    Level = level,
                    Closeness = tameness,
                    Fullness = fullness,
                    Summoned = false,
                    Flag = 0
                };
                dbContext.Pets.Add(dbModel);
                dbContext.SaveChanges();
                return dbModel.Petid;
            }
            catch (Exception e)
            {
                petLog.Error(e.ToString());
                return -1;
            }
        }

        public static void deleteFromDb(IPlayer owner, int petid)
        {
            try
            {
                using var dbContext = new DBContext();
                dbContext.Pets.Where(x => x.Petid == petid).ExecuteDelete();

                owner.resetExcluded(petid);
                CashIdGenerator.freeCashId(petid);
            }
            catch (Exception ex)
            {
                petLog.Error(ex.ToString());
            }
        }
        #endregion

        public static void UpdateEquip(IPlayer player, int newStat, int newSpdJmp)
        {
            Inventory equip = player.getInventory(InventoryType.EQUIP);

            for (byte i = 1; i <= equip.getSlotLimit(); i++)
            {
                try
                {
                    var eq = (Equip?)equip.getItem(i);
                    if (eq == null)
                    {
                        continue;
                    }

                    SetEquipStat(eq, newStat, newSpdJmp);

                    player.forceUpdateItem(eq);
                }
                catch (Exception e)
                {
                    player.Log.Error(e.ToString());
                }
            }
        }

        public static void SetEquipStat(Equip equip, int stat, int spdjmp)
        {
            equip.setStr(stat);
            equip.setDex(stat);
            equip.setInt(stat);
            equip.setLuk(stat);
            equip.setMatk(stat);
            equip.setWatk(stat);
            equip.setAcc(stat);
            equip.setAvoid(stat);
            equip.setJump(spdjmp);
            equip.setSpeed(spdjmp);
            equip.setWdef(stat);
            equip.setMdef(stat);
            equip.setHp(stat);
            equip.setMp(stat);

            short flag = equip.getFlag();
            flag |= ItemConstants.UNTRADEABLE;
            equip.setFlag(flag);
        }
    }
}
