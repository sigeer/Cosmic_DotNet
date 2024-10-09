using Application.Core.EF.Entities;
using client;
using constants.game;
using server;
using tools;

namespace Application.Core.Game.Players
{
    public partial class Player
    {
        public AtomicInteger ExpValue { get; set; }
        public void gainExp(int gain)
        {
            gainExp(gain, true, true);
        }

        public void gainExp(int gain, bool show, bool inChat, bool white = true)
        {
            gainExp(gain, 0, show, inChat, white);
        }

        public void gainExp(int gain, int party, bool show, bool inChat, bool white)
        {
            if (hasDisease(Disease.CURSE))
            {
                gain *= (int)0.5;
                party *= (int)0.5;
            }

            if (gain < 0)
            {
                gain = int.MaxValue;   // integer overflow, heh.
            }

            if (party < 0)
            {
                party = int.MaxValue;  // integer overflow, heh.
            }

            int equip = (int)Math.Min((long)(gain / 10) * pendantExp, int.MaxValue);

            gainExpInternal(gain, equip, party, show, inChat, white);
        }


        public void loseExp(int loss, bool show, bool inChat, bool white = true)
        {
            gainExpInternal(-loss, 0, 0, show, inChat, white);
        }

        private void announceExpGain(long gain, int equip, int party, bool inChat, bool white)
        {
            gain = Math.Min(gain, int.MaxValue);
            if (gain == 0)
            {
                if (party == 0)
                {
                    return;
                }

                gain = party;
                party = 0;
                white = false;
            }

            sendPacket(PacketCreator.getShowExpGain((int)gain, equip, party, inChat, white));
        }

        object gainExpLock = new object();
        private void gainExpInternal(long gain, int equip, int party, bool show, bool inChat, bool white)
        {   // need of method synchonization here detected thanks to MedicOP
            lock (gainExpLock)
            {
                long total = Math.Max(gain + equip + party, -ExpValue.get());

                if (Level < getMaxLevel() && (allowExpGain || this.getEventInstance() != null))
                {
                    long leftover = 0;
                    long nextExp = ExpValue.get() + total;

                    if (nextExp > int.MaxValue)
                    {
                        total = int.MaxValue - ExpValue.get();
                        leftover = nextExp - int.MaxValue;
                    }
                    updateSingleStat(Stat.EXP, ExpValue.addAndGet((int)total));
                    totalExpGained += total;
                    if (show)
                    {
                        announceExpGain(gain, equip, party, inChat, white);
                    }
                    while (ExpValue.get() >= ExpTable.getExpNeededForLevel(Level))
                    {
                        levelUp(true);
                        if (Level == getMaxLevel())
                        {
                            setExp(0);
                            updateSingleStat(Stat.EXP, 0);
                            break;
                        }
                    }

                    if (leftover > 0)
                    {
                        gainExpInternal(leftover, equip, party, false, inChat, white);
                    }
                    else
                    {
                        lastExpGainTime = DateTimeOffset.Now;

                        if (YamlConfig.config.server.USE_EXP_GAIN_LOG)
                        {
                            ExpLogRecord expLogRecord = new ExpLogRecord(
                                Id,
                                getWorldServer().ExpRate,
                                expCoupon,
                                totalExpGained,
                                ExpValue.get(),
                                lastExpGainTime
                            );
                            ExpLogger.putExpLogRecord(expLogRecord);
                        }

                        totalExpGained = 0;
                    }
                }
            }

        }
    }
}
