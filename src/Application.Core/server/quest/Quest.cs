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


using Application.Core.Game.Packets;
using Application.Core.Game.QuestDomain;
using Application.Core.Game.QuestDomain.RequirementAdapter;
using Application.Core.Game.QuestDomain.RewardAdapter;
using client;
using server.quest.actions;
using server.quest.requirements;
using tools;
using static Application.Core.Game.Players.Player;
using static client.QuestStatus;

namespace server.quest;

/**
 * @author Matze
 * @author Ronan - support for medal quests
 */
public class Quest
{
    private static volatile Dictionary<int, Quest> quests = new();
    private static volatile Dictionary<int, int> infoNumberQuests = new();
    private static Dictionary<short, int> medals = new();

    private static HashSet<short> exploitableQuests = new();

    static Quest()
    {
        exploitableQuests.Add(2338);    // there are a lot more exploitable quests, they need to be nit-picked
        exploitableQuests.Add(3637);
        exploitableQuests.Add(3714);
        exploitableQuests.Add(21752);
    }

    protected short id;
    protected int timeLimit, timeLimit2;
    protected Dictionary<QuestRequirementType, AbstractQuestRequirement> startReqs = new();
    protected Dictionary<QuestRequirementType, AbstractQuestRequirement> completeReqs = new();
    protected Dictionary<QuestActionType, AbstractQuestAction> startActs = new();
    protected Dictionary<QuestActionType, AbstractQuestAction> completeActs = new();
    protected List<int> relevantMobs = new();
    private bool autoStart;
    private bool autoPreComplete, autoComplete;
    private bool repeatable = false;
    private string name = "", parent = "";
    private static DataProvider questData = DataProviderFactory.getDataProvider(WZFiles.QUEST);
    private static Data questInfo = questData.getData("QuestInfo.img");
    private static Data questAct = questData.getData("Act.img");
    private static Data questCheck = questData.getData("Check.img");

    private Quest(Data? reqInfo, short? questId = null)
    {
        if (reqInfo == null && questId == null)
            throw new BusinessException();

        string idString;
        if (reqInfo == null)
        {
            this.id = questId!.Value;
            idString = this.id.ToString();
            reqInfo = questInfo?.getChildByPath(idString);
        }
        else
        {
            idString = reqInfo.getName()!;
            this.id = short.Parse(idString);
        }

        var checkData = questCheck.getChildByPath(id.ToString());
        if (checkData == null)
        {
            LogFactory.ResLogger.Error("QuestInfo: Id = {QuestId} not found in Check.img", id);
            return;
        }

        if (reqInfo != null)
        {
            name = DataTool.getString("name", reqInfo) ?? "";
            parent = DataTool.getString("parent", reqInfo) ?? "";

            timeLimit = DataTool.getInt("timeLimit", reqInfo, 0);
            timeLimit2 = DataTool.getInt("timeLimit2", reqInfo, 0);
            autoStart = DataTool.getInt("autoStart", reqInfo, 0) == 1;
            autoPreComplete = DataTool.getInt("autoPreComplete", reqInfo, 0) == 1;
            autoComplete = DataTool.getInt("autoComplete", reqInfo, 0) == 1;

            int medalid = DataTool.getInt("viewMedalItem", reqInfo, 0);
            if (medalid != 0)
            {
                medals.AddOrUpdate(this.id, medalid);
            }
        }
        else
        {
            LogFactory.ResLogger.Error("QuestInfo: Id = {QuestId} not found in QuestInfo.img", id);
        }

        var startReqData = checkData.getChildByPath("0");
        if (startReqData != null)
        {
            foreach (Data startReq in startReqData.getChildren())
            {
                QuestRequirementType type = QuestRequirementTypeUtils.getByWZName(startReq.getName());
                switch (type)
                {
                    case QuestRequirementType.INTERVAL:
                        repeatable = true;
                        break;
                    case QuestRequirementType.MOB:
                        foreach (Data mob in startReq.getChildren())
                        {
                            relevantMobs.Add(DataTool.getInt(mob.getChildByPath("id")));
                        }
                        break;
                }

                var req = this.getRequirement(type, startReq);
                if (req == null)
                {
                    continue;
                }

                startReqs.AddOrUpdate(type, req);
            }
        }

        var completeReqData = checkData.getChildByPath("1");
        if (completeReqData != null)
        {
            foreach (Data completeReq in completeReqData.getChildren())
            {
                QuestRequirementType type = QuestRequirementTypeUtils.getByWZName(completeReq.getName());

                var req = this.getRequirement(type, completeReq);
                if (req == null)
                {
                    continue;
                }

                if (type.Equals(QuestRequirementType.MOB))
                {
                    foreach (Data mob in completeReq.getChildren())
                    {
                        relevantMobs.Add(DataTool.getInt(mob.getChildByPath("id")));
                    }
                }
                completeReqs.AddOrUpdate(type, req);
            }
        }
        var actData = questAct.getChildByPath(id.ToString());
        if (actData == null)
        {
            return;
        }
        var startActData = actData.getChildByPath("0");
        if (startActData != null)
        {
            foreach (Data startAct in startActData.getChildren())
            {
                QuestActionType questActionType = QuestActionTypeUtils.getByWZName(startAct.getName());
                var act = this.getAction(questActionType, startAct);

                if (act == null)
                {
                    continue;
                }

                startActs.AddOrUpdate(questActionType, act);
            }
        }
        var completeActData = actData.getChildByPath("1");
        if (completeActData != null)
        {
            foreach (Data completeAct in completeActData.getChildren())
            {
                QuestActionType questActionType = QuestActionTypeUtils.getByWZName(completeAct.getName());
                var act = this.getAction(questActionType, completeAct);

                if (act == null)
                {
                    continue;
                }

                completeActs.AddOrUpdate(questActionType, act);
            }
        }
    }
    public Quest(QuestEntity questInfo, List<QuestRequirementEntity> reqs, List<QuestRewardEntity> rewards)
    {
        var checkData = questCheck.getChildByPath(id.ToString());
        if (checkData == null)
        {
            LogFactory.ResLogger.Error("QuestInfo: Id = {QuestId} not found in Check.img", id);
            return;
        }
        id = (short)questInfo.Id;
        name = questInfo.Name;
        parent = questInfo.ParentName;
        timeLimit = questInfo.TimeLimit;
        timeLimit2 = questInfo.TimeLimit2;
        autoStart = questInfo.AutoStart;
        autoPreComplete = questInfo.AutoPreComplete;
        autoComplete = questInfo.AutoComplete;
        medals[id] = questInfo.MedalId;


        foreach (var startReq in reqs.Where(x => x.Step == 0))
        {
            QuestRequirementType type = QuestRequirementTypeUtils.getByWZName(startReq.RequirementType);
            switch (type)
            {
                case QuestRequirementType.INTERVAL:
                    repeatable = true;
                    break;
                case QuestRequirementType.MOB:
                    if (int.TryParse(startReq.Value, out var d))
                        relevantMobs.Add(d);
                    break;
            }

            var req = GetRequirement(type, startReq);
            if (req == null)
            {
                continue;
            }

            startReqs.AddOrUpdate(type, req);
        }

        foreach (var completeReq in reqs.Where(x => x.Step == 1))
        {
            QuestRequirementType type = QuestRequirementTypeUtils.getByWZName(completeReq.RequirementType);

            var req = GetRequirement(type, completeReq);
            if (req == null)
            {
                continue;
            }

            if (type.Equals(QuestRequirementType.MOB) && int.TryParse(completeReq.Value, out var d))
            {
                relevantMobs.Add(d);
            }
            completeReqs.AddOrUpdate(type, req);
        }

        foreach (var startAct in rewards.Where(x => x.Step == 0))
        {
            QuestActionType questActionType = QuestActionTypeUtils.getByWZName(startAct.RewardType);
            var act = GetReward(questActionType, startAct);

            if (act != null)
            {
                startActs.AddOrUpdate(questActionType, act);
            }
        }

        foreach (var completeAct in rewards.Where(x => x.Step == 1))
        {
            QuestActionType questActionType = QuestActionTypeUtils.getByWZName(completeAct.RewardType);
            var act = GetReward(questActionType, completeAct);

            if (act != null)
            {
                completeActs.AddOrUpdate(questActionType, act);
            }
        }
    }
    public bool isAutoComplete()
    {
        return autoPreComplete || autoComplete;
    }

    public bool isAutoStart()
    {
        return autoStart;
    }


    public static Quest getInstance(int id)
    {
        var ret = quests.GetValueOrDefault(id);
        if (ret == null)
        {
            ret = QuestFromDB.LoadQuestFromDB(id);
            if (ret == null)
                ret = new Quest(null, (short)id);
            quests.AddOrUpdate(id, ret);
        }
        return ret;
    }

    public static Quest getInstanceFromInfoNumber(int infoNumber)
    {
        return getInstance(infoNumberQuests.GetValueOrDefault(infoNumber, infoNumber));
    }

    public bool isSameDayRepeatable()
    {
        if (!repeatable)
        {
            return false;
        }

        var ir = startReqs.GetValueOrDefault(QuestRequirementType.INTERVAL) as IntervalRequirement;
        return ir != null && ir.getInterval() < TimeSpan.FromHours(YamlConfig.config.server.QUEST_POINT_REPEATABLE_INTERVAL).TotalMilliseconds;
    }

    public bool canStartQuestByStatus(IPlayer chr)
    {
        QuestStatus mqs = chr.getQuest(this);
        return !(!mqs.getStatus().Equals(Status.NOT_STARTED) && !(mqs.getStatus().Equals(Status.COMPLETED) && repeatable));
    }

    public bool canQuestByInfoProgress(IPlayer chr)
    {
        QuestStatus mqs = chr.getQuest(this);
        List<string> ix = mqs.getInfoEx();
        if (ix.Count > 0)
        {
            short questid = mqs.getQuestID();
            short infoNumber = mqs.getInfoNumber();
            if (infoNumber <= 0)
            {
                infoNumber = questid;  // on default infoNumber mimics questid
            }

            int ixSize = ix.Count;
            for (int i = 0; i < ixSize; i++)
            {
                string progress = chr.getAbstractPlayerInteraction().getQuestProgress(infoNumber, i);
                string ixProgress = ix.get(i);

                if (progress != (ixProgress))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool canStart(IPlayer chr, int npcid)
    {
        if (!canStartQuestByStatus(chr))
        {
            return false;
        }

        foreach (var r in startReqs.Values)
        {
            if (!r.check(chr, npcid))
            {
                return false;
            }
        }

        return canQuestByInfoProgress(chr);
    }

    public bool canComplete(IPlayer chr, int? npcid)
    {
        QuestStatus mqs = chr.getQuest(this);
        if (!mqs.getStatus().Equals(Status.STARTED))
        {
            return false;
        }

        foreach (var r in completeReqs.Values)
        {
            if (!r.check(chr, npcid))
            {
                return false;
            }
        }

        return canQuestByInfoProgress(chr);
    }

    public void start(IPlayer chr, int npc)
    {
        if (autoStart || canStart(chr, npc))
        {
            var acts = startActs.Values;
            foreach (var a in acts)
            {
                if (!a.check(chr, null))
                { // would null be good ?
                    return;
                }
            }
            foreach (var a in acts)
            {
                a.run(chr, null);
            }
            forceStart(chr, npc);
        }
    }

    public void complete(IPlayer chr, int npc)
    {
        complete(chr, npc, null);
    }

    public void complete(IPlayer chr, int npc, int? selection)
    {
        if (autoPreComplete || canComplete(chr, npc))
        {
            var acts = completeActs.Values;
            foreach (var a in acts)
            {
                if (!a.check(chr, selection))
                {
                    return;
                }
            }
            forceComplete(chr, npc);
            foreach (var a in acts)
            {
                a.run(chr, selection);
            }
            if (!this.hasNextQuestAction())
            {
                chr.announceUpdateQuest(DelayedQuestUpdate.INFO, chr.getQuest(this));
            }
        }
    }

    public void reset(IPlayer chr)
    {
        QuestStatus newStatus = new QuestStatus(this, QuestStatus.Status.NOT_STARTED);
        chr.updateQuestStatus(newStatus);
    }

    public bool forfeit(IPlayer chr)
    {
        if (!chr.getQuest(this).getStatus().Equals(Status.STARTED))
        {
            return false;
        }
        if (timeLimit > 0)
        {
            chr.sendPacket(QuestPacket.RemoveQuestTimeLimit(id));
        }
        QuestStatus newStatus = new QuestStatus(this, QuestStatus.Status.NOT_STARTED);
        newStatus.setForfeited(chr.getQuest(this).getForfeited() + 1);
        chr.updateQuestStatus(newStatus);
        return true;
    }

    public bool forceStart(IPlayer chr, int npc)
    {
        QuestStatus newStatus = new QuestStatus(this, QuestStatus.Status.STARTED, npc);

        QuestStatus oldStatus = chr.getQuest(this.getId());
        foreach (var e in oldStatus.getProgress())
        {
            newStatus.setProgress(e.Key, e.Value);
        }

        if (id / 100 == 35 && YamlConfig.config.server.TOT_MOB_QUEST_REQUIREMENT > 0)
        {
            int setProg = 999 - Math.Min(999, YamlConfig.config.server.TOT_MOB_QUEST_REQUIREMENT);

            foreach (int pid in newStatus.getProgress().Keys)
            {
                if (pid >= 8200000 && pid <= 8200012)
                {
                    string pr = StringUtil.getLeftPaddedStr(setProg.ToString(), '0', 3);
                    newStatus.setProgress(pid, pr);
                }
            }
        }

        newStatus.setForfeited(chr.getQuest(this).getForfeited());
        newStatus.setCompleted(chr.getQuest(this).getCompleted());

        if (timeLimit > 0)
        {
            newStatus.setExpirationTime(DateTimeOffset.Now.AddSeconds(timeLimit).ToUnixTimeMilliseconds());
            chr.questTimeLimit(this, timeLimit);
        }
        if (timeLimit2 > 0)
        {
            newStatus.setExpirationTime(DateTimeOffset.Now.AddSeconds(timeLimit2).ToUnixTimeMilliseconds());
            chr.questTimeLimit2(this, newStatus.getExpirationTime());
        }

        chr.updateQuestStatus(newStatus);

        return true;
    }

    public bool forceComplete(IPlayer chr, int npc)
    {
        if (timeLimit > 0)
        {
            chr.sendPacket(QuestPacket.RemoveQuestTimeLimit(id));
        }

        QuestStatus newStatus = new QuestStatus(this, QuestStatus.Status.COMPLETED, npc);
        newStatus.setForfeited(chr.getQuest(this).getForfeited());
        newStatus.setCompleted(chr.getQuest(this).getCompleted());
        newStatus.setCompletionTime(DateTimeOffset.Now.ToUnixTimeMilliseconds());
        chr.updateQuestStatus(newStatus);

        chr.sendPacket(PacketCreator.showSpecialEffect(9)); // Quest completion
        chr.getMap().broadcastMessage(chr, PacketCreator.showForeignEffect(chr.getId(), 9), false); //use 9 instead of 12 for both
        return true;
    }

    public short getId()
    {
        return id;
    }

    public List<int> getRelevantMobs()
    {
        return relevantMobs;
    }

    public int getStartItemAmountNeeded(int itemid)
    {
        var req = startReqs.GetValueOrDefault(QuestRequirementType.ITEM);
        if (req is ItemRequirement ireq)
            return ireq.getItemAmountNeeded(itemid, false);
        return int.MinValue;

    }

    public int getCompleteItemAmountNeeded(int itemid)
    {
        var req = completeReqs.GetValueOrDefault(QuestRequirementType.ITEM);
        if (req is ItemRequirement ireq)
            return ireq.getItemAmountNeeded(itemid, true);
        return int.MaxValue;
    }

    public int getMobAmountNeeded(int mid)
    {
        var req = completeReqs.GetValueOrDefault(QuestRequirementType.MOB);
        if (req is MobRequirement mreq)
            return mreq.getRequiredMobCount(mid);
        return 0;
    }

    public short getInfoNumber(Status qs)
    {
        bool checkEnd = qs.Equals(Status.STARTED);
        Dictionary<QuestRequirementType, AbstractQuestRequirement> reqs = !checkEnd ? startReqs : completeReqs;

        var req = reqs.GetValueOrDefault(QuestRequirementType.INFO_NUMBER);
        if (req is InfoNumberRequirement inReq)
            return inReq.getInfoNumber();
        return 0;
    }

    public string getInfoEx(Status qs, int index)
    {
        return getInfoEx(qs).ElementAtOrDefault(index) ?? "";
    }

    public List<string> getInfoEx(Status qs)
    {
        bool checkEnd = qs.Equals(Status.STARTED);
        Dictionary<QuestRequirementType, AbstractQuestRequirement> reqs = !checkEnd ? startReqs : completeReqs;
        var req = reqs.GetValueOrDefault(QuestRequirementType.INFO_EX);
        if (req is InfoExRequirement ixReq)
            return ixReq.getInfo();
        return new();
    }

    public int getTimeLimit()
    {
        return timeLimit;
    }

    public static void clearCache(int quest)
    {
        quests.Remove(quest);
    }

    public static void clearCache()
    {
        quests.Clear();
    }

    private AbstractQuestRequirement? GetRequirement(QuestRequirementType type, QuestRequirementEntity data)
    {
        AbstractQuestRequirement? ret = null;
        switch (type)
        {
            case QuestRequirementType.END_DATE:
                ret = new EndDateRequirement(new EntityRequirementDataAdapter(data));
                break;
            case QuestRequirementType.JOB:
                ret = new JobRequirement(new EntityRequirementJobAdapter(data));
                break;
            case QuestRequirementType.QUEST:
                ret = new QuestRequirement(new EntityRequirementQuestAdapter(data));
                break;
            case QuestRequirementType.FIELD_ENTER:
                ret = new FieldEnterRequirement(new EntityRequirementFieldEnterAdapter(data));
                break;
            case QuestRequirementType.INFO_NUMBER:
                ret = new InfoNumberRequirement(new EntityRequirementDataAdapter(data));
                break;
            case QuestRequirementType.INFO_EX:
                ret = new InfoExRequirement(new EntityRequirementInfoExAdapter(data));
                break;
            case QuestRequirementType.INTERVAL:
                ret = new IntervalRequirement(getId(), new EntityRequirementDataAdapter(data));
                break;
            case QuestRequirementType.COMPLETED_QUEST:
                ret = new CompletedQuestRequirement(new EntityRequirementDataAdapter(data));
                break;
            case QuestRequirementType.ITEM:
                ret = new ItemRequirement(new EntityRequirementItemAdapter(data));
                break;
            case QuestRequirementType.MAX_LEVEL:
                ret = new MaxLevelRequirement(new EntityRequirementDataAdapter(data));
                break;
            case QuestRequirementType.MESO:
                ret = new MesoRequirement(new EntityRequirementDataAdapter(data));
                break;
            case QuestRequirementType.MIN_LEVEL:
                ret = new MinLevelRequirement(new EntityRequirementDataAdapter(data));
                break;
            case QuestRequirementType.MIN_PET_TAMENESS:
                ret = new MinTamenessRequirement(new EntityRequirementDataAdapter(data));
                break;
            case QuestRequirementType.MOB:
                ret = new MobRequirement(getId(), new EntityRequirementMobAdapter(data));
                break;
            case QuestRequirementType.MONSTER_BOOK:
                ret = new MonsterBookCountRequirement(new EntityRequirementDataAdapter(data));
                break;
            case QuestRequirementType.NPC:
                ret = new NpcRequirement(new EntityRequirementDataAdapter(data));
                break;
            case QuestRequirementType.PET:
                ret = new PetRequirement(new EntityRequirementPetAdapter(data));
                break;
            case QuestRequirementType.BUFF:
                ret = new BuffRequirement(new EntityRequirementDataAdapter(data));
                break;
            case QuestRequirementType.EXCEPT_BUFF:
                ret = new BuffExceptRequirement(new EntityRequirementDataAdapter(data));
                break;
            case QuestRequirementType.SCRIPT:
                ret = new ScriptRequirement(new EntityRequirementDataAdapter(data));
                break;
            case QuestRequirementType.NORMAL_AUTO_START:
            case QuestRequirementType.START:
            case QuestRequirementType.END:
                break;
            default:
                //FilePrinter.printError(FilePrinter.EXCEPTION_CAUGHT, "Unhandled Requirement Type: " + type.ToString() + " QuestID: " + this.getId());
                break;
        }
        return ret;
    }

    private AbstractQuestRequirement? getRequirement(QuestRequirementType type, Data data)
    {
        AbstractQuestRequirement? ret = null;
        switch (type)
        {
            case QuestRequirementType.END_DATE:
                ret = new EndDateRequirement(new WzRequirementDataAdapter(data));
                break;
            case QuestRequirementType.JOB:
                ret = new JobRequirement(new WzRequirementJobAdapter(data));
                break;
            case QuestRequirementType.QUEST:
                ret = new QuestRequirement(new WzRequirementQuestAdapter(data));
                break;
            case QuestRequirementType.FIELD_ENTER:
                ret = new FieldEnterRequirement(new WzRequirementFieldEnterAdapter(data));
                break;
            case QuestRequirementType.INFO_NUMBER:
                ret = new InfoNumberRequirement(new WzRequirementDataAdapter(data));
                break;
            case QuestRequirementType.INFO_EX:
                ret = new InfoExRequirement(new WzRequirementInfoExAdapter(data));
                break;
            case QuestRequirementType.INTERVAL:
                ret = new IntervalRequirement(getId(), new WzRequirementDataAdapter(data));
                break;
            case QuestRequirementType.COMPLETED_QUEST:
                ret = new CompletedQuestRequirement(new WzRequirementDataAdapter(data));
                break;
            case QuestRequirementType.ITEM:
                ret = new ItemRequirement(new WzRequirementItemAdapter(data));
                break;
            case QuestRequirementType.MAX_LEVEL:
                ret = new MaxLevelRequirement(new WzRequirementDataAdapter(data));
                break;
            case QuestRequirementType.MESO:
                ret = new MesoRequirement(new WzRequirementDataAdapter(data));
                break;
            case QuestRequirementType.MIN_LEVEL:
                ret = new MinLevelRequirement(new WzRequirementDataAdapter(data));
                break;
            case QuestRequirementType.MIN_PET_TAMENESS:
                ret = new MinTamenessRequirement(new WzRequirementDataAdapter(data));
                break;
            case QuestRequirementType.MOB:
                ret = new MobRequirement(getId(), new WzRequirementMobAdapter(data));
                break;
            case QuestRequirementType.MONSTER_BOOK:
                ret = new MonsterBookCountRequirement(new WzRequirementDataAdapter(data));
                break;
            case QuestRequirementType.NPC:
                ret = new NpcRequirement(new WzRequirementDataAdapter(data));
                break;
            case QuestRequirementType.PET:
                ret = new PetRequirement(new WzRequirementPetAdapter(data));
                break;
            case QuestRequirementType.BUFF:
                ret = new BuffRequirement(new WzRequirementDataAdapter(data));
                break;
            case QuestRequirementType.EXCEPT_BUFF:
                ret = new BuffExceptRequirement(new WzRequirementDataAdapter(data));
                break;
            case QuestRequirementType.SCRIPT:
                ret = new ScriptRequirement(new WzRequirementDataAdapter(data));
                break;
            case QuestRequirementType.NORMAL_AUTO_START:
            case QuestRequirementType.START:
            case QuestRequirementType.END:
                break;
            default:
                //FilePrinter.printError(FilePrinter.EXCEPTION_CAUGHT, "Unhandled Requirement Type: " + type.ToString() + " QuestID: " + this.getId());
                break;
        }
        return ret;
    }

    private AbstractQuestAction? GetReward(QuestActionType type, QuestRewardEntity data)
    {
        AbstractQuestAction? ret = null;
        switch (type)
        {
            case QuestActionType.BUFF:
                ret = new BuffAction(new EntityRewardDataAdapter(data), this);
                break;
            case QuestActionType.EXP:
                ret = new ExpAction(new EntityRewardDataAdapter(data), this);
                break;
            case QuestActionType.FAME:
                ret = new FameAction(new EntityRewardDataAdapter(data), this);
                break;
            case QuestActionType.ITEM:
                ret = new ItemAction(new EntityRewardItemAdapter(data), this);
                break;
            case QuestActionType.MESO:
                ret = new MesoAction(new EntityRewardDataAdapter(data), this);
                break;
            case QuestActionType.NEXTQUEST:
                ret = new NextQuestAction(new EntityRewardDataAdapter(data), this);
                break;
            case QuestActionType.PETSKILL:
                ret = new PetSkillAction(new EntityRewardDataAdapter(data), this);
                break;
            case QuestActionType.QUEST:
                ret = new QuestAction(new EntityRewardQuestAdapter(data), this);
                break;
            case QuestActionType.SKILL:
                ret = new SkillAction(new EntityRewardSkillAdapter(data), this);
                break;
            case QuestActionType.PETTAMENESS:
                ret = new PetTamenessAction(new EntityRewardDataAdapter(data), this);
                break;
            case QuestActionType.PETSPEED:
                ret = new PetSpeedAction(new EntityRewardDataAdapter(data), this);
                break;
            case QuestActionType.INFO:
                ret = new InfoAction(new EntityRewardDataAdapter(data), this);
                break;
            default:
                //FilePrinter.printError(FilePrinter.EXCEPTION_CAUGHT, "Unhandled Action Type: " + type.ToString() + " QuestID: " + this.getId());
                break;
        }
        return ret;
    }

    private AbstractQuestAction? getAction(QuestActionType type, Data data)
    {
        AbstractQuestAction? ret = null;
        switch (type)
        {
            case QuestActionType.BUFF:
                ret = new BuffAction(new WzRewardDataAdapter(data), this);
                break;
            case QuestActionType.EXP:
                ret = new ExpAction(new WzRewardDataAdapter(data), this);
                break;
            case QuestActionType.FAME:
                ret = new FameAction(new WzRewardDataAdapter(data), this);
                break;
            case QuestActionType.ITEM:
                ret = new ItemAction(new WzRewardItemAdapter(data), this);
                break;
            case QuestActionType.MESO:
                ret = new MesoAction(new WzRewardDataAdapter(data), this);
                break;
            case QuestActionType.NEXTQUEST:
                ret = new NextQuestAction(new WzRewardDataAdapter(data), this);
                break;
            case QuestActionType.PETSKILL:
                ret = new PetSkillAction(new WzPetSkillRewardAdapter(data), this);
                break;
            case QuestActionType.QUEST:
                ret = new QuestAction(new WzRewardQuestAdapter(data), this);
                break;
            case QuestActionType.SKILL:
                ret = new SkillAction(new WzRewardSkillAdapter(data), this);
                break;
            case QuestActionType.PETTAMENESS:
                ret = new PetTamenessAction(new WzRewardDataAdapter(data), this);
                break;
            case QuestActionType.PETSPEED:
                ret = new PetSpeedAction(new WzRewardDataAdapter(data), this);
                break;
            case QuestActionType.INFO:
                ret = new InfoAction(new WzRewardDataAdapter(data), this);
                break;
            default:
                //FilePrinter.printError(FilePrinter.EXCEPTION_CAUGHT, "Unhandled Action Type: " + type.ToString() + " QuestID: " + this.getId());
                break;
        }
        return ret;
    }

    public bool restoreLostItem(IPlayer chr, int itemid)
    {
        if (chr.getQuest(this).getStatus().Equals(QuestStatus.Status.STARTED))
        {
            var itemAct = startActs.GetValueOrDefault(QuestActionType.ITEM) as ItemAction;
            if (itemAct != null)
            {
                return itemAct.restoreLostItem(chr, itemid);
            }
        }

        return false;
    }

    public int getMedalRequirement()
    {
        return medals.GetValueOrDefault(id, -1);
    }

    public int getNpcRequirement(bool checkEnd)
    {
        Dictionary<QuestRequirementType, AbstractQuestRequirement> reqs = !checkEnd ? startReqs : completeReqs;
        var mqr = reqs.GetValueOrDefault(QuestRequirementType.NPC);

        if (mqr is NpcRequirement n)
            return n.get();
        return -1;
    }

    public bool hasScriptRequirement(bool checkEnd)
    {
        Dictionary<QuestRequirementType, AbstractQuestRequirement> reqs = !checkEnd ? startReqs : completeReqs;
        var mqr = reqs.GetValueOrDefault(QuestRequirementType.SCRIPT);

        return mqr is ScriptRequirement s && s.get();
    }

    public bool hasNextQuestAction()
    {
        Dictionary<QuestActionType, AbstractQuestAction> acts = completeActs;
        var mqa = acts.GetValueOrDefault(QuestActionType.NEXTQUEST);

        return mqa != null;
    }

    public string getName()
    {
        return name;
    }

    public string getParentName()
    {
        return parent;
    }

    public static bool isExploitableQuest(short questid)
    {
        return exploitableQuests.Contains(questid);
    }

    public static List<Quest> getMatchedQuests(string search)
    {
        List<Quest> ret = new();

        search = search.ToLower();
        foreach (Quest mq in quests.Values)
        {
            if (mq.name.ToLower().Contains(search) || mq.parent.ToLower().Contains(search))
            {
                ret.Add(mq);
            }
        }

        return ret;
    }

    public static void loadAllQuests()
    {
        var allWZData = questInfo.getChildren();
        LogFactory.ResLogger.Debug($"QuestCount: {allWZData.Count}");

        Dictionary<int, Quest> loadedQuests = new();
        Dictionary<int, int> loadedInfoNumberQuests = new();

        foreach (Data quest in allWZData)
        {
            Quest q = new Quest(quest);
            int questID = q.getId();
            loadedQuests.AddOrUpdate(questID, q);

            int infoNumber;

            infoNumber = q.getInfoNumber(Status.STARTED);
            if (infoNumber > 0)
            {
                loadedInfoNumberQuests.AddOrUpdate(infoNumber, questID);
            }

            infoNumber = q.getInfoNumber(Status.COMPLETED);
            if (infoNumber > 0)
            {
                loadedInfoNumberQuests.AddOrUpdate(infoNumber, questID);
            }
        }

        Quest.quests = loadedQuests;
        Quest.infoNumberQuests = loadedInfoNumberQuests;

        foreach (var q in QuestFromDB.LoadQuestFromDB())
        {
            var questId = q.getId();
            if (quests.ContainsKey(questId))
                throw new Exception($"QuestId 重复： {questId}");

            quests[questId] = q;

            int infoNumber;

            infoNumber = q.getInfoNumber(Status.STARTED);
            if (infoNumber > 0)
            {
                infoNumberQuests.AddOrUpdate(infoNumber, questId);
            }

            infoNumber = q.getInfoNumber(Status.COMPLETED);
            if (infoNumber > 0)
            {
                infoNumberQuests.AddOrUpdate(infoNumber, questId);
            }

        }
    }
}
