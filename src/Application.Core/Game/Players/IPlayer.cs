using Application.Core.client.Characters;
using Application.Core.Game.Items;
using Application.Core.Game.Life;
using Application.Core.Game.Maps;
using Application.Core.Game.Maps.AnimatedObjects;
using Application.Core.Game.Relation;
using Application.Core.Game.Skills;
using Application.Core.Game.TheWorld;
using Application.Core.scripting.Event;
using client;
using client.autoban;
using client.creator;
using client.inventory;
using client.keybind;
using client.newyear;
using net.packet;
using net.server;
using net.server.world;
using scripting;
using scripting.Event;
using server;
using server.events;
using server.events.gm;
using server.life;
using server.maps;
using server.minigame;
using server.partyquest;
using server.quest;
using static Application.Core.Game.Players.Player;

namespace Application.Core.Game.Players
{
    public interface IPlayer : IDB_Character, IAnimatedMapObject, IMapObject, IPlayerStats, IMapPlayer
    {
        public IClient Client { get; }
        public int? Channel { get; set; }
        public bool IsOnlined => Client.IsGameOnlined;
        public BuddyList BuddyList { get; set; }
        public PlayerBag Bag { get; set; }
        public Storage Storage { get; set; }
        public CashShop CashShopModel { get; }
        public SavedLocation?[] SavedLocations { get; set; }
        public Dictionary<int, KeyBinding> KeyMap { get; set; }
        public Dictionary<Skill, SkillEntry> Skills { get; set; }
        public SkillMacro?[] SkillMacros { get; set; }

        public ITeam? TeamModel { get; }
        public IGuild? GuildModel { get; }
        public IAlliance? AllianceModel { get; }
        public ISchool? SchoolModel { get; set; }
        public Dictionary<short, string> AreaInfo { get; set; }
        public MonsterBook Monsterbook { get; set; }
        public Messenger? Messenger { get; set; }

        public IMount? MountModel { get; }
        public Job JobModel { get; set; }
        public SkinColor SkinColorModel { get; set; }

        public List<int> TrockMaps { get; set; }
        public List<int> VipTrockMaps { get; set; }
        public Dictionary<string, Events> Events { get; set; }

        public byte[]? QuickSlotLoaded { get; set; }
        public QuickslotBinding? QuickSlotKeyMapped { get; set; }
        public Fitness? Fitness { get ; set ; }
        public Ola? Ola { get; set; }

        public object SaveToDBLock { get; set; }

        public event EventHandler<IPlayer>? OnLevelUp;
        public event EventHandler<IPlayer>? OnJobUpdate;
        public event EventHandler<IPlayer>? OnLodgedUpdate;

        public ILogger Log { get; }

        void StartPlayerTask();
        void StopPlayerTask();
        void addCooldown(int skillId, long startTime, long length);
        void addCP(int ammount);
        void addCrushRing(Ring r);
        int addDojoPointsByMap(int mapid);
        void addExcluded(int petId, int x);
        void addFame(int famechange);
        void addFriendshipRing(Ring r);
        void addGachaExp(int gain);
        void addJailExpirationTime(long time);
        void addMarriageRing(Ring? r);
        void addMerchantMesos(int add);
        void addMesosTraded(int gain);
        void addNewYearRecord(NewYearCardRecord newyear);
        void addPet(Pet pet);
        void addPlayerRing(Ring ring);
        void addSummon(int id, Summon summon);
        void addTrockMap();
        void addVipTrockMap();
        void addVisibleMapObject(IMapObject mo);
        void announceBattleshipHp();
        void announceDiseases();
        void announceUpdateQuest(DelayedQuestUpdate questUpdateType, params object[] paramsValue);
        bool applyConsumeOnPickup(int itemId);
        bool applyHpMpChange(int hpCon, int hpchange, int mpchange);
        void applyPartyDoor(Door door, bool partyUpdate);
        bool attemptCatchFish(int baitLevel);
        void autoban(string reason);
        void awardQuestPoint(int awardedPoints);
        void ban(string reason);
        void block(int reason, int days, string desc);
        void blockPortal(string? scriptName);
        void unblockPortal(string? scriptName);
        void broadcastAcquaintances(int type, string message);
        void broadcastAcquaintances(Packet packet);
        void broadcastMarriageMessage();
        void broadcastStance();
        void broadcastStance(int newStance);
        void buffExpireTask();
        int calculateMaxBaseDamage(int watk);
        int calculateMaxBaseDamage(int watk, WeaponType weapon);
        int calculateMaxBaseMagicDamage(int matk);
        void cancelAllBuffs(bool softcancel);
        void cancelAllDebuffs();
        void cancelBuffExpireTask();
        void cancelBuffStats(BuffStat stat);
        void cancelDiseaseExpireTask();
        void cancelEffect(int itemId);
        bool cancelEffect(StatEffect effect, bool overwrite, long startTime);
        void cancelEffectFromBuffStat(BuffStat stat);
        void cancelExpirationTask();
        void cancelMagicDoor();
        bool cancelPendingNameChange();
        bool cancelPendingWorldTranfer();
        void cancelQuestExpirationTask();
        void cancelSkillCooldownTask();
        bool canDoor();
        bool canGainSlots(int type, int slots);
        FameStatus canGiveFame(IPlayer from);
        bool canHold(int itemid);
        bool canHold(int itemid, int quantity);
        bool canHoldMeso(int gain);
        bool canHoldUniques(List<int> itemids);
        bool cannotEnterCashShop();
        void changeCI(int type);
        void changeFaceExpression(int emote);
        void changeJob(Job? newJob);
        void changeKeybinding(int key, KeyBinding keybinding);


        void changePage(int page);
        void changeQuickslotKeybinding(byte[] aQuickslotKeyMapped);
        void changeSkillLevel(Skill skill, sbyte newLevel, int newMasterlevel, long expiration);
        void changeTab(int tab);
        void changeType(int type);
        void checkBerserk(bool isHidden);
        void checkMessenger();
        int checkWorldTransferEligibility();
        void clearCpqTimer();
        void clearSavedLocation(SavedLocationType type);
        void clearSummons();
        void closeHiredMerchant(bool closeMerchant);
        void closeMiniGame(bool forceClose);
        void closeNpcShop();
        void closePartySearchInteractions();
        void closePlayerInteractions();
        void closePlayerMessenger();
        void closePlayerShop();
        void closeRPS();
        void closeTrade();
        void collectDiseases();
        void commitExcludedItems();
        bool containsAreaInfo(int area, string info);
        bool containsSummon(Summon summon);
        void controlMonster(Monster monster);
        int countItem(int itemid);
        void createDragon();
        void debugListAllBuffs();
        void debugListAllBuffsCount();
        void decreaseBattleshipHp(int decrease);
        void decreaseReports();
        void deleteBuddy(int otherCid);
        void deleteFromTrocks(int map);
        void deleteFromVipTrocks(int map);
        void disablePartySearchInvite(int fromLeaderid);
        void disbandGuild();
        void diseaseExpireTask();
        void dispel();
        void dispelBuffCoupons();
        void dispelDebuff(Disease debuff);
        void dispelDebuffs();
        void dispelSkill(int skillid);
        void doHurtHp();
        void doPendingNameChange();
        void dropMessage(int type, string message);
        void dropMessage(string message);
        void empty(bool remove);

        void equipChanged();
        void equippedItem(Equip equip);
        void expirationTask();
        void exportExcludedItems(IClient c);
        int fetchDoorSlot();
        void flushDelayedUpdateQuests();

        void forceUpdateItem(Item item);
        void forfeitExpirableQuests();
        void gainAriantPoints(int points);
        void gainCP(int gain);
        void gainExp(int gain);
        void gainExp(int gain, bool show, bool inChat, bool white = true);
        void gainExp(int gain, int party, bool show, bool inChat, bool white);
        void gainFame(int delta);
        bool gainFame(int delta, IPlayer? fromPlayer, int mode);
        void gainFestivalPoints(int gain);
        void gainGachaExp();
        void gainMeso(int gain);
        void gainMeso(int gain, bool show);
        void gainMeso(int gain, bool show, bool enableActions, bool inChat);
        bool gainSlots(int type, int slots);
        bool gainSlots(int type, int slots, bool update);

        // IPlayer generateCharacterEntry();
        void genericGuildMessage(int code);
        AbstractPlayerInteraction getAbstractPlayerInteraction();
        int getAccountID();
        IReadOnlyCollection<int> getActiveCoupons();
        List<PlayerBuffValueHolder> getAllBuffs();
        List<PlayerCoolDownValueHolder> getAllCooldowns();
        Dictionary<Disease, DiseaseExpiration> getAllDiseases();
        Alliance? getAlliance();
        int getAllianceRank();
        string? getAreaInfo(int area);
        Dictionary<short, string> getAreaInfos();
        AriantColiseum? getAriantColiseum();
        int getAriantPoints();
        AutobanManager getAutobanManager();
        float getAutopotHpAlert();
        float getAutopotMpAlert();
        HashSet<int> getAvailableBuffs();
        int getAvailableCP();
        int getBattleshipHp();
        List<string> getBlockedPortals();
        int getBossDropRate();
        BuddyList getBuddylist();
        long? getBuffedStarttime(BuffStat effect);
        int? getBuffedValue(BuffStat effect);
        StatEffect? getBuffEffect(BuffStat stat);
        int getBuffSource(BuffStat stat);
        float getCardRate(int itemid);
        CashShop getCashShop();
        int getChair();
        string? getChalkboard();
        IWorldChannel getChannelServer();
        int getCleanItemQuantity(int itemid, bool checkEquipped);
        IClient getClient();
        short getCombo();
        List<QuestStatus> getCompletedQuests();
        ICollection<Monster> getControlledMonsters();
        int getCouponDropRate();
        int getCouponExpRate();
        int getCouponMesoRate();
        int getCP();
        List<Ring> getCrushRings();
        bool getCS();
        int getCurrentCI();
        int getCurrentPage();
        int getCurrentTab();
        int getCurrentType();
        int getDiseasesSize();
        int getDojoEnergy();
        int getDojoPoints();
        int getDojoStage();
        ICollection<Door> getDoors();
        int getDoorSlot();
        Dragon? getDragon();
        int getDropRate();
        int getEnergyBar();
        EventInstanceManager? getEventInstance();
        Dictionary<string, Events> getEvents();
        Dictionary<int, HashSet<int>> getExcluded();
        HashSet<int> getExcludedItems();
        int getExp();
        int getExpRate();
        int getFace();
        int getFame();
        Family? getFamily();
        FamilyEntry? getFamilyEntry();
        int getFamilyId();
        int getFestivalPoints();
        int getFh();
        bool getFinishedDojoTutorial();
        List<Ring> getFriendshipRings();
        int getGachaExp();
        int getGender();
        IGuild? getGuild();
        int getGuildId();
        int getGuildRank();
        int getHair();
        HiredMerchant? getHiredMerchant();
        int getId();
        int getInitialSpawnpoint();
        Inventory getInventory(InventoryType type);
        int getItemEffect();
        int getItemQuantity(int itemid, bool checkEquipped);
        long getJailExpirationTimeLeft();
        Job getJob();
        int getJobRank();
        int getJobRankMove();
        Job getJobStyle();
        Job getJobStyle(byte opt);
        int getJobType();
        Dictionary<int, KeyBinding> getKeymap();
        int getLanguage();
        long getLastCombo();
        string getLastCommandMessage();
        long getLastHealed();
        int getLastMobCount();
        long getLastSnowballAttack();
        long getLastUsedCashItem();
        List<int> getLastVisitedMapids();
        int getLevel();
        int getLinkedLevel();
        string? getLinkedName();
        TimeSpan getLoggedInTime();
        DateTimeOffset getLoginTime();
        SkillMacro?[] getMacros();
        Door? getMainTownDoor();
        int getMapId();
        Marriage? getMarriageInstance();
        int getMarriageItemId();
        Ring? getMarriageRing();
        int getMasterLevel(int skill);
        int getMasterLevel(Skill? skill);
        int getMaxClassLevel();
        int getMaxLevel();
        string getMedalText();
        int getMerchantMeso();
        int getMerchantNetMeso();
        int getMeso();
        int getMesoRate();
        int getMesosTraded();
        Messenger? getMessenger();
        int getMessengerPosition();
        int getMiniGamePoints(MiniGame.MiniGameResult type, bool omok);
        MonsterBook getMonsterBook();
        int getMonsterBookCover();
        MonsterCarnival? getMonsterCarnival();
        MonsterCarnivalParty? getMonsterCarnivalParty();
        void SetMount(IMount? mount);
        IMount? getMount();
        string getName();
        NewYearCardRecord? getNewYearRecord(int cardid);
        HashSet<NewYearCardRecord> getNewYearRecords();
        int getNextEmptyPetIndex();
        int getNoPets();
        long getNpcCooldown();
        int getNumControlledMonsters();
        int getOwlSearch();
        IMap? getOwnedMap();
        int getPartnerId();
        ITeam? getParty();
        /// <summary>
        /// 不存在队伍时为 -1
        /// </summary>
        /// <returns></returns>
        int getPartyId();
        List<IPlayer> getPartyMembersOnline();
        List<IPlayer> getPartyMembersOnSameMap();
        PartyQuest? getPartyQuest();
        string getPartyQuestItems();
        Pet? getPet(int index);
        sbyte getPetIndex(int petId);
        sbyte getPetIndex(Pet pet);
        Pet?[] getPets();
        Door? getPlayerDoor();
        PlayerShop? getPlayerShop();
        int getPossibleReports();
        QuestStatus getQuest(int quest);
        QuestStatus getQuest(Quest quest);
        int getQuestExpRate();
        int getQuestMesoRate();
        QuestStatus? getQuestNAdd(Quest quest);
        QuestStatus? getQuestNoAdd(Quest quest);
        QuestStatus? getQuestRemove(Quest quest);
        byte getQuestStatus(int quest);
        int getRank();
        int getRankMove();
        int getRawDropRate();
        int getRawExpRate();
        int getRawMesoRate();
        HashSet<NewYearCardRecord> getReceivedNewYearRecords();
        int getRelationshipId();
        int getRemainingSp();
        int getRewardPoints();
        Ring? getRingById(int id);
        RockPaperScissor? getRPS();
        int getSavedLocation(string type);
        string? getSearch();
        Shop? getShop();
        long getSkillExpiration(int skill);
        long getSkillExpiration(Skill? skill);
        int getSkillLevel(int skill);
        sbyte getSkillLevel(Skill? skill);
        Dictionary<Skill, SkillEntry> getSkills();
        SkinColor getSkinColor();
        int getSlot();
        byte getSlots(int type);
        List<QuestStatus> getStartedQuests();
        StatEffect? getStatForBuff(BuffStat effect);
        Storage getStorage();
        Summon? getSummonByKey(int id);
        ICollection<Summon> getSummonsValues();
        int getTargetHpBarHash();
        long getTargetHpBarTime();
        sbyte getTeam();
        int getTotalCP();
        int getTotalDex();
        int getTotalInt();
        int getTotalLuk();
        int getTotalMagic();
        int getTotalStr();
        int getTotalWatk();
        Trade? getTrade();
        List<int> getTrockMaps();
        int getTrockSize();
        int getVanquisherKills();
        int getVanquisherStage();
        List<int> getVipTrockMaps();
        int getVipTrockSize();
        IMapObject[] getVisibleMapObjects();
        IMap getWarpMap(int map);
        bool getWhiteChat();
        int getWorld();
        IWorld getWorldServer();
        void giveCoolDowns(int skillid, long starttime, long length);
        void giveDebuff(Disease disease, MobSkill skill);
        int gmLevel();
        bool gotPartyQuestItem(string partyquestchar);
        void handleEnergyChargeGain();
        void handleOrbconsume();
        bool hasActiveBuff(int sourceid);
        bool hasBuffFromSourceid(int sourceid);
        bool hasDisabledPartySearchInvite(int fromLeaderid);
        bool hasDisease(Disease dis);
        bool hasEmptySlot(int itemId);
        bool hasEmptySlot(sbyte invType);
        bool hasEntered(string script);
        bool hasEntered(string script, int mapId);
        void hasGivenFame(IPlayer to);
        bool hasJustMarried();
        bool hasMerchant();
        bool hasNoviceExpRate();
        bool haveCleanItem(int itemid);
        bool haveItem(int itemid);
        bool haveItemEquipped(int itemid);
        bool haveItemWithId(int itemid, bool checkEquipped);
        bool haveWeddingRing();
        void Hide(bool hide);
        void Hide(bool hide, bool login);
        void increaseEquipExp(int expGain);
        void increaseGuildCapacity();
        bool insertNewChar(CharacterFactoryRecipe recipe);
        bool isAran();

        bool isBanned();
        bool isBeginnerJob();
        bool isBuffFrom(BuffStat stat, Skill skill);
        bool isChallenged();
        bool isChangingMaps();
        bool isChasing();
        bool isCygnus();
        bool isEquippedItemPouch();
        bool isEquippedMesoMagnet();
        bool isEquippedPetItemIgnore();
        bool isGM();
        bool isGmJob();
        bool isGuildLeader();
        bool isHidden();
        /// <summary>
        /// 已登录
        /// </summary>
        /// <returns></returns>
        bool isLoggedin();
        /// <summary>
        /// 已登录 且 不在商城/拍卖
        /// </summary>
        /// <returns></returns>
        bool isLoggedinWorld();
        /// <summary>
        /// 在商城/拍卖
        /// </summary>
        /// <returns></returns>
        bool isAwayFromWorld();
        void setAwayFromChannelWorld();
        void setEnteredChannelWorld(int channel);
        bool isMale();
        bool isMapObjectVisible(IMapObject mo);
        bool isMarried();
        bool isPartyLeader();
        bool isPartyMember(IPlayer chr);
        bool isPartyMember(int cid);
        bool isRecvPartySearchInviteEnabled();
        bool isRidingBattleship();
        bool isSummonsEmpty();
        bool isTrockMap(int id);
        bool isVipTrockMap(int id);
        void leaveMap();
        bool leaveParty();
        void levelUp(bool takeexp);
        void logOff();
        void loseExp(int loss, bool show, bool inChat, bool white = true);
        bool mergeAllItemsFromName(string name);
        void mergeAllItemsFromPosition(Dictionary<Equip.StatUpgrade, float> statups, short pos);
        void message(string m);
        IMount mount(int id, int skillid);
        bool needQuestItem(int questid, int itemid);
        void newClient(IClient c);
        void notifyMapTransferToPartner(int mapid);
        void partyOperationUpdate(ITeam party, List<IPlayer>? exPartyMembers);
        int peekSavedLocation(string type);
        void pickupItem(IMapObject ob);
        void pickupItem(IMapObject ob, int petIndex);
        long portalDelay();
        void portalDelay(long delay);
        void purgeDebuffs();
        void questExpirationTask();
        void questTimeLimit(Quest quest, int seconds);
        void questTimeLimit2(Quest quest, long expires);
        void raiseQuestMobCount(int id);
        void receivePartyMemberHP();
        bool registerChairBuff();
        void registerEffect(StatEffect effect, long starttime, long expirationtime, bool isSilent);
        bool registerNameChange(string newName);
        bool registerWorldTransfer(int newWorld);
        void releaseControlledMonsters();
        void reloadQuestExpirations();
        void removeAllCooldownsExcept(int id, bool packet);
        void removeCooldown(int skillId);
        void removeIncomingInvites();
        void removeJailExpirationTime();
        void removeNewYearRecord(NewYearCardRecord newyear);
        Door? removePartyDoor(bool partyUpdate);
        void removePartyDoor(ITeam formerParty);
        void removePartyQuestItem(string letter);
        void removePet(Pet pet, bool shift_left);
        void removeSandboxItems();
        void removeVisibleMapObject(IMapObject mo);
        void resetBattleshipHp();
        void resetCP();
        void resetEnteredScript();
        void resetEnteredScript(int mapId);
        void resetEnteredScript(string script);
        void resetExcluded(int petId);
        void resetPartySearchInvite(int fromLeaderid);
        void resetPlayerAggro();
        void resetPlayerRates();
        void resetStats();
        void respawn(EventInstanceManager? eim, int returnMap);
        void respawn(int returnMap);
        void revertLastPlayerRates();
        void revertPlayerRates();
        void revertWorldRates();
        void runFullnessSchedule(int petSlot);
        bool runTirednessSchedule();
        //void saveCharToDB();
        void saveCharToDB(bool notAutosave = true);
        void saveCooldowns();
        void saveGuildStatus();
        void saveLocation(string type);
        void saveLocationOnWarp();
        int sellAllItemsFromName(sbyte invTypeId, string name);
        int sellAllItemsFromPosition(ItemInformationProvider ii, InventoryType type, short pos);
        void sendKeymap();
        void sendMacros();
        void sendPacket(Packet packet);
        void sendPolice(int greason, string reason, int duration);
        void sendPolice(string text);
        void sendQuickmap();

        void setAllianceRank(int _rank);
        void setAriantColiseum(AriantColiseum? ariantColiseum);
        void setAutopotHpAlert(float hpPortion);
        void setAutopotMpAlert(float mpPortion);

        void setBattleshipHp(int battleshipHp);
        void setBuddyCapacity(int capacity);
        void setBuffedValue(BuffStat effect, int value);
        void setChalkboard(string? text);
        void setChallenged(bool challenged);
        void setChasing(bool chasing);
        void setClient(IClient c);
        void setCombo(short count);
        void setCP(int a);
        void setCpqTimer(ScheduledFuture timer);
        void setCS(bool cs);
        void setDisconnectedFromChannelWorld();
        void setDojoEnergy(int x);
        void setDojoPoints(int x);
        void setDojoStage(int x);
        void setDragon(Dragon dragon);
        void setEnergyBar(int set);

        void setEventInstance(EventInstanceManager? eventInstance);
        void setExp(int amount);
        void setFace(int face);
        void setFame(int fame);
        void setFamilyEntry(FamilyEntry? entry);
        void setFamilyId(int familyId);
        void setFestivalPoints(int pontos);
        void setFinishedDojoTutorial();
        void setGachaExp(int amount);
        void setGender(int gender);
        void setGM(int level);
        void setGMLevel(int level);
        void setGuildId(int _id);
        void setGuildRank(int _rank);
        void setHair(int hair);
        void setHasMerchant(bool set);
        void setHasSandboxItem();
        void setHiredMerchant(HiredMerchant? merchant);
        void setInventory(InventoryType type, Inventory inv);
        void setItemEffect(int itemEffect);
        void setJob(Job job);
        void setLanguage(int num);
        void setLastCombo(long time);
        void setLastCommandMessage(string text);
        void setLastHealed(long time);
        void setLastMobCount(byte count);
        void setLastSnowballAttack(long time);
        void setLastUsedCashItem(long time);
        void setLevel(int level);
        void setLoginTime(DateTimeOffset time);
        void setMap(int PmapId);
        void setMapId(int mapid);
        void setMapTransitionComplete();
        void setMarriageItemId(int itemid);
        void setMasteries(int jobId);
        void setMerchantMeso(int set);
        void setMessenger(Messenger? messenger);
        void setMessengerPosition(int position);
        MiniGame? getMiniGame();
        void setMiniGame(MiniGame? miniGame);
        void setMiniGamePoints(IPlayer visitor, int winnerslot, bool omok);
        void setMonsterBookCover(int bookCover);
        void setMonsterCarnival(MonsterCarnival? monsterCarnival);
        void setMonsterCarnivalParty(MonsterCarnivalParty? mcp);
        void setName(string name);
        void setNpcCooldown(long d);
        // void setOfflineGuildRank(int newRank);
        void setOwlSearch(int id);
        void setOwnedMap(IMap? map);
        void setPartnerId(int partnerid);
        void setParty(ITeam? p);
        void setPartyQuest(PartyQuest? pq);
        void setPartyQuestItemObtained(string partyquestchar);
        void setPlayerAggro(int mobHash);
        void setPlayerRates();
        void setPlayerShop(PlayerShop? playerShop);
        void setQuestAdd(Quest quest, byte status, string customData);
        void setQuestProgress(int id, int infoNumber, string progress);
        void setRewardPoints(int value);
        void setRPS(RockPaperScissor? rps);
        void setSearch(string? find);
        void setSessionTransitionState();
        void setShop(Shop? shop);
        void setSkinColor(SkinColor skinColor);
        void setSlot(int slotid);
        void setTargetHpBarHash(int mobHash);
        void setTargetHpBarTime(long timeNow);
        void setTeam(int team);
        void setTotalCP(int a);
        void setTrade(Trade? trade);
        void setVanquisherKills(int x);
        void setVanquisherStage(int x);
        void setWorld(int world);
        void setWorldRates();
        void shiftPetsRight();
        void showAllEquipFeatures();
        void showDojoClock();
        void showHint(string msg);
        void showHint(string msg, int length);

        void showUnderleveledInfo(Monster mob);
        void silentApplyDiseases(Dictionary<Disease, DiseaseExpiration> diseaseMap);
        void silentGiveBuffs(List<KeyValuePair<long, PlayerBuffValueHolder>> buffs);
        void silentPartyUpdate();
        void sitChair(int itemId);
        void skillCooldownTask();
        bool skillIsCooling(int skillId);

        void stopControllingMonster(Monster monster);
        void toggleBlockCashShop();
        void toggleExpGain();
        void toggleHide(bool login);
        bool toggleRecvPartySearchInvite();
        void toggleWhiteChat();
        void unequipAllPets();
        void unequippedItem(Equip equip);
        void unequipPet(Pet pet, bool shift_left);
        void unequipPet(Pet pet, bool shift_left, bool hunger);
        bool unregisterChairBuff();
        void updateActiveEffects();
        void updateAreaInfo(int area, string info);
        void updateAriantScore();
        void updateAriantScore(int dropQty);
        void updateCouponRates();
        void updateMacros(int position, SkillMacro? updateMacro);
        void updatePartyMemberHP();
        void updatePartySearchAvailability(bool psearchAvailable);
        void updateQuestStatus(QuestStatus qs);
        void updateRemainingSp(int remainingSp);
        void updateSingleStat(Stat stat, int newval);
        void useCP(int ammount);

        void withdrawMerchantMesos();
        void yellowMessage(string m);

        List<QuestStatus> getQuests();
    }
}
