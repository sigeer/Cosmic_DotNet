using scripting.Event;
using YamlDotNet.Core;

namespace Application.Core.Game.GameEvents.PartyQuest
{
    public abstract class PlayerPartyQuestBase
    {
        public string EventName { get; set; }
        public string EventFamily { get; set; }

        public int MinCount { get; set; }
        public int MaxCount { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public int EntryNpcId { get; set; }
        public int FirstMapId { get; set; }
        public int EndMapId { get; set; }
        public abstract int ClearMapId { get; }
        public IPlayer Player { get; }
        public EventManager EventManager => Player.getChannelServer().getEventSM().getEventManager(EventName) ?? throw new BusinessResException($"{EventName} 对应的脚本没有找到");
        protected PlayerPartyQuestBase(string name, string evtFamily, IPlayer player)
        {
            EventName = name;
            EventFamily = evtFamily;
            Player = player;

            var currentJs = EventManager.getIv();
            MinCount = Convert.ToInt32(currentJs.GetValue("minPlayers"));
            MaxCount = Convert.ToInt32(currentJs.GetValue("maxPlayers"));
            MinLevel = Convert.ToInt32(currentJs.GetValue("minLevel"));
            MaxLevel = Convert.ToInt32(currentJs.GetValue("maxLevel"));
            FirstMapId = Convert.ToInt32(currentJs.GetValue("entryMap"));
            EndMapId = Convert.ToInt32(currentJs.GetValue("clearMap"));
        }
        public void StartQuest()
        {
            if (!Player.isPartyLeader())
            {
                Player.dropMessage(1, "只有队长才能开启任务");
                return;
            }

            var effectTeam = FilterTeam();
            var teamEffectCount = effectTeam.Count;
            if (teamEffectCount < MinCount || teamEffectCount > MaxCount)
            {
                Player.dropMessage(1, "队伍人数或者等级不符合要求");
                return;
            }

            Player.TeamModel!.setEligibleMembers(effectTeam);
            if (!EventManager.startInstance(Player.TeamModel!, Player.MapModel, 1))
            {
                Player.dropMessage(1, "当前频道已经有一个队伍正在进行任务");
                return;
            }
        }

        protected virtual void PassStage(EventInstanceManager eim, int curStg, int curMapId)
        {
            eim.setProperty(curStg + "stageclear", "true");
            eim.showClearEffect(true);

            eim.linkToNextStage(curStg, EventFamily, curMapId);  //opens the portal to the next map
        }

        public void CompleteStage()
        {
            var eim = Player.getEventInstance();
            if (eim != null)
            {
                var curMapId = Player.getMapId();
                if (curMapId <= ClearMapId)
                {
                    var curStg = GetStageFromMap(curMapId);
                    PassStage(eim, curStg, curMapId);

                    if (curMapId == ClearMapId)
                        CompleteQuest(eim);
                }
                else
                {
                    Player.dropMessage("自行离场");
                }
            }
            else
            {
                Player.dropMessage(1, "没有任务");
            }
        }

        public virtual void CompleteQuest(EventInstanceManager eim)
        {
            eim.clearPQ();
        }

        protected virtual List<IPlayer> FilterTeam()
        {
            return Player.TeamModel!.getPartyMembersOnline().Where(x => x.Level >= MinLevel && x.Level <= MaxLevel).ToList();
        }

        public abstract int GetStageFromMap(int mapId);
    }

    public record MapStage(int MapId, int Stage);
}
