using Application.Core.EF.Entities;
using Application.Core.EF.Entities.Gachapons;
using Application.Core.Game.QuestDomain;
using Microsoft.EntityFrameworkCore;

namespace Application.EF;

public partial class DBContext : DbContext
{
    public DBContext()
    {
    }

    public DBContext(DbContextOptions<DBContext> options)
        : base(options)
    {
    }

    #region Entities
    public DbSet<QuestEntity> Quests { get; set; }
    public DbSet<QuestRequirementEntity> QuestRequirements { get; set; }
    public DbSet<QuestRewardEntity> QuestRewards { get; set; }
    public DbSet<GachaponPoolLevelChance> GachaponPoolLevelChances { get; set; }
    public DbSet<GachaponPool> GachaponPools { get; set; }
    public DbSet<GachaponPoolItem> GachaponPoolItems { get; set; }
    public virtual DbSet<ExpLogRecord> ExpLogRecords { get; set; }
    public virtual DbSet<AccountEntity> Accounts { get; set; }

    public virtual DbSet<AllianceEntity> Alliances { get; set; }

    public virtual DbSet<Allianceguild> AllianceGuilds { get; set; }

    public virtual DbSet<AreaInfo> AreaInfos { get; set; }

    public virtual DbSet<BbsReply> BbsReplies { get; set; }

    public virtual DbSet<BbsThread> BbsThreads { get; set; }

    public virtual DbSet<BosslogDaily> BosslogDailies { get; set; }

    public virtual DbSet<BosslogWeekly> BosslogWeeklies { get; set; }

    public virtual DbSet<Buddy> Buddies { get; set; }

    public virtual DbSet<CharacterEntity> Characters { get; set; }

    public virtual DbSet<Cooldown> Cooldowns { get; set; }

    public virtual DbSet<DropDataGlobal> DropDataGlobals { get; set; }

    public virtual DbSet<DropData> DropData { get; set; }

    public virtual DbSet<Dueyitem> Dueyitems { get; set; }

    public virtual DbSet<Dueypackage> Dueypackages { get; set; }

    public virtual DbSet<Eventstat> Eventstats { get; set; }

    public virtual DbSet<Famelog> Famelogs { get; set; }

    public virtual DbSet<FamilyCharacter> FamilyCharacters { get; set; }

    public virtual DbSet<DB_FamilyEntitlement> FamilyEntitlements { get; set; }

    public virtual DbSet<Fredstorage> Fredstorages { get; set; }

    public virtual DbSet<Gift> Gifts { get; set; }

    public virtual DbSet<GuildEntity> Guilds { get; set; }

    public virtual DbSet<Hwidaccount> Hwidaccounts { get; set; }

    public virtual DbSet<Hwidban> Hwidbans { get; set; }

    public virtual DbSet<Inventoryequipment> Inventoryequipments { get; set; }

    public virtual DbSet<Inventoryitem> Inventoryitems { get; set; }

    public virtual DbSet<Inventorymerchant> Inventorymerchants { get; set; }

    public virtual DbSet<Ipban> Ipbans { get; set; }

    public virtual DbSet<Keymap> Keymaps { get; set; }

    public virtual DbSet<Macban> Macbans { get; set; }

    public virtual DbSet<Macfilter> Macfilters { get; set; }

    public virtual DbSet<Makercreatedatum> Makercreatedata { get; set; }

    public virtual DbSet<Makerreagentdatum> Makerreagentdata { get; set; }

    public virtual DbSet<Makerrecipedatum> Makerrecipedata { get; set; }

    public virtual DbSet<Makerrewarddatum> Makerrewarddata { get; set; }

    public virtual DbSet<DB_Marriage> Marriages { get; set; }

    public virtual DbSet<Medalmap> Medalmaps { get; set; }

    public virtual DbSet<Monsterbook> Monsterbooks { get; set; }

    public virtual DbSet<Monstercarddatum> Monstercarddata { get; set; }

    public virtual DbSet<MtsCart> MtsCarts { get; set; }

    public virtual DbSet<MtsItem> MtsItems { get; set; }

    public virtual DbSet<Namechange> Namechanges { get; set; }

    public virtual DbSet<Newyear> Newyears { get; set; }

    public virtual DbSet<DB_Note> Notes { get; set; }

    public virtual DbSet<Nxcode> Nxcodes { get; set; }

    public virtual DbSet<NxcodeItem> NxcodeItems { get; set; }

    public virtual DbSet<Nxcoupon> Nxcoupons { get; set; }

    public virtual DbSet<PetEntity> Pets { get; set; }

    public virtual DbSet<Petignore> Petignores { get; set; }

    public virtual DbSet<Playerdisease> Playerdiseases { get; set; }

    public virtual DbSet<Playernpc> Playernpcs { get; set; }

    public virtual DbSet<PlayernpcsEquip> PlayernpcsEquips { get; set; }

    public virtual DbSet<PlayernpcsField> PlayernpcsFields { get; set; }

    public virtual DbSet<Plife> Plives { get; set; }


    public virtual DbSet<Questprogress> Questprogresses { get; set; }

    public virtual DbSet<QuestStatusEntity> Queststatuses { get; set; }

    public virtual DbSet<Quickslotkeymapped> Quickslotkeymappeds { get; set; }

    public virtual DbSet<Reactordrop> Reactordrops { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Response> Responses { get; set; }

    public virtual DbSet<Ring_Entity> Rings { get; set; }

    public virtual DbSet<SavedLocationEntity> Savedlocations { get; set; }

    public virtual DbSet<ServerQueue> ServerQueues { get; set; }

    public virtual DbSet<DB_Shop> Shops { get; set; }

    public virtual DbSet<Shopitem> Shopitems { get; set; }

    public virtual DbSet<SkillEntity> Skills { get; set; }

    public virtual DbSet<Skillmacro> Skillmacros { get; set; }

    public virtual DbSet<Specialcashitem> Specialcashitems { get; set; }

    public virtual DbSet<DB_Storage> Storages { get; set; }

    public virtual DbSet<Trocklocation> Trocklocations { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    public virtual DbSet<Worldtransfer> Worldtransfers { get; set; }
    #endregion

    //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //        => optionsBuilder.UseMySQL();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySQL(YamlConfig.config.server.DB_CONNECTIONSTRING);
        base.OnConfiguring(optionsBuilder);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("accounts");

            entity.HasIndex(e => new { e.Id, e.Name }, "id");

            entity.HasIndex(e => new { e.Id, e.NxCredit, e.MaplePoint, e.NxPrepaid }, "id_2");

            entity.HasIndex(e => e.Name, "name").IsUnique();

            entity.HasIndex(e => new { e.Id, e.Banned }, "ranking1");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Banned).HasColumnName("banned");
            entity.Property(e => e.Banreason)
                .HasColumnType("text")
                .HasColumnName("banreason");
            entity.Property(e => e.Birthday)
                .HasDefaultValueSql("'2005-05-11'")
                .HasColumnType("date")
                .HasColumnName("birthday");
            entity.Property(e => e.Characterslots)
                .HasDefaultValueSql("'3'")
                .HasColumnType("tinyint(2)")
                .HasColumnName("characterslots");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("createdat");
            entity.Property(e => e.Email)
                .HasMaxLength(45)
                .HasColumnName("email");
            entity.Property(e => e.Gender)
                .HasDefaultValueSql("'10'")
                .HasColumnType("tinyint(2)")
                .HasColumnName("gender");
            entity.Property(e => e.Greason)
                .HasColumnType("tinyint(4)")
                .HasColumnName("greason");
            entity.Property(e => e.Hwid)
                .HasMaxLength(12)
                .HasDefaultValueSql("''")
                .HasColumnName("hwid");
            entity.Property(e => e.Ip)
                .HasColumnType("text")
                .HasColumnName("ip");
            entity.Property(e => e.Language)
                .HasDefaultValueSql("'2'")
                .HasColumnType("int(1)")
                .HasColumnName("language");
            entity.Property(e => e.Lastlogin)
                .HasColumnType("timestamp")
                .HasColumnName("lastlogin");
            entity.Property(e => e.Loggedin)
                .HasColumnType("tinyint(4)")
                .HasColumnName("loggedin");
            entity.Property(e => e.Macs)
                .HasColumnType("tinytext")
                .HasColumnName("macs");
            entity.Property(e => e.MaplePoint)
                .HasColumnType("int(11)")
                .HasColumnName("maplePoint");
            entity.Property(e => e.Mute)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(1)")
                .HasColumnName("mute");
            entity.Property(e => e.Name)
                .HasMaxLength(13)
                .HasDefaultValueSql("''")
                .HasColumnName("name");
            entity.Property(e => e.Nick)
                .HasMaxLength(20)
                .HasColumnName("nick");
            entity.Property(e => e.NxCredit)
                .HasColumnType("int(11)")
                .HasColumnName("nxCredit");
            entity.Property(e => e.NxPrepaid)
                .HasColumnType("int(11)")
                .HasColumnName("nxPrepaid");
            entity.Property(e => e.Password)
                .HasMaxLength(128)
                .HasDefaultValueSql("''")
                .HasColumnName("password");
            entity.Property(e => e.Pic)
                .HasMaxLength(26)
                .HasDefaultValueSql("''")
                .HasColumnName("pic");
            entity.Property(e => e.Pin)
                .HasMaxLength(10)
                .HasDefaultValueSql("''")
                .HasColumnName("pin");
            entity.Property(e => e.Rewardpoints)
                .HasColumnType("int(11)")
                .HasColumnName("rewardpoints");
            entity.Property(e => e.Sitelogged)
                .HasColumnType("text")
                .HasColumnName("sitelogged");
            entity.Property(e => e.Tempban)
                .HasDefaultValueSql("'2005-05-11 00:00:00'")
                .HasColumnType("timestamp")
                .HasColumnName("tempban");
            entity.Property(e => e.Tos).HasColumnName("tos");
            entity.Property(e => e.Votepoints)
                .HasColumnType("int(11)")
                .HasColumnName("votepoints");
            entity.Property(e => e.Webadmin)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(1)")
                .HasColumnName("webadmin");
        });

        modelBuilder.Entity<AllianceEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("alliance");

            entity.HasIndex(e => e.Name, "name");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Capacity)
                .HasDefaultValueSql("'2'")
                .HasColumnType("int(10) unsigned")
                .HasColumnName("capacity");
            entity.Property(e => e.Name)
                .HasMaxLength(13)
                .HasColumnName("name");
            entity.Property(e => e.Notice)
                .HasMaxLength(20)
                .HasDefaultValueSql("''")
                .HasColumnName("notice");
            entity.Property(e => e.Rank1)
                .HasMaxLength(11)
                .HasDefaultValueSql("'Master'")
                .HasColumnName("rank1");
            entity.Property(e => e.Rank2)
                .HasMaxLength(11)
                .HasDefaultValueSql("'Jr. Master'")
                .HasColumnName("rank2");
            entity.Property(e => e.Rank3)
                .HasMaxLength(11)
                .HasDefaultValueSql("'Member'")
                .HasColumnName("rank3");
            entity.Property(e => e.Rank4)
                .HasMaxLength(11)
                .HasDefaultValueSql("'Member'")
                .HasColumnName("rank4");
            entity.Property(e => e.Rank5)
                .HasMaxLength(11)
                .HasDefaultValueSql("'Member'")
                .HasColumnName("rank5");
        });

        modelBuilder.Entity<Allianceguild>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("allianceguilds");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.AllianceId)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("int(10)")
                .HasColumnName("allianceid");
            entity.Property(e => e.GuildId)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("int(10)")
                .HasColumnName("guildid");
        });

        modelBuilder.Entity<AreaInfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("area_info");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Area)
                .HasColumnType("int(11)")
                .HasColumnName("area");
            entity.Property(e => e.Charid)
                .HasColumnType("int(11)")
                .HasColumnName("charid");
            entity.Property(e => e.Info)
                .HasMaxLength(200)
                .HasColumnName("info");
        });

        modelBuilder.Entity<BbsReply>(entity =>
        {
            entity.HasKey(e => e.Replyid).HasName("PRIMARY");

            entity.ToTable("bbs_replies");

            entity.Property(e => e.Replyid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("replyid");
            entity.Property(e => e.Content)
                .HasMaxLength(26)
                .HasDefaultValueSql("''")
                .HasColumnName("content");
            entity.Property(e => e.Postercid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("postercid");
            entity.Property(e => e.Threadid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("threadid");
            entity.Property(e => e.Timestamp)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("timestamp");
        });

        modelBuilder.Entity<BbsThread>(entity =>
        {
            entity.HasKey(e => e.Threadid).HasName("PRIMARY");

            entity.ToTable("bbs_threads");

            entity.Property(e => e.Threadid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("threadid");
            entity.Property(e => e.Guildid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("guildid");
            entity.Property(e => e.Icon)
                .HasColumnType("smallint(5) unsigned")
                .HasColumnName("icon");
            entity.Property(e => e.Localthreadid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("localthreadid");
            entity.Property(e => e.Name)
                .HasMaxLength(26)
                .HasDefaultValueSql("''")
                .HasColumnName("name");
            entity.Property(e => e.Postercid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("postercid");
            entity.Property(e => e.Replycount)
                .HasColumnType("smallint(5) unsigned")
                .HasColumnName("replycount");
            entity.Property(e => e.Startpost)
                .HasColumnType("text")
                .HasColumnName("startpost");
            entity.Property(e => e.Timestamp)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("timestamp");
        });

        modelBuilder.Entity<BosslogDaily>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("bosslog_daily");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Attempttime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("attempttime");
            entity.Property(e => e.Bosstype)
                .HasColumnType("enum('ZAKUM','HORNTAIL','PINKBEAN','SCARGA','PAPULATUS')")
                .HasColumnName("bosstype");
            entity.Property(e => e.CharacterId)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
        });

        modelBuilder.Entity<BosslogWeekly>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("bosslog_weekly");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Attempttime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("attempttime");
            entity.Property(e => e.Bosstype)
                .HasColumnType("enum('ZAKUM','HORNTAIL','PINKBEAN','SCARGA','PAPULATUS')")
                .HasColumnName("bosstype");
            entity.Property(e => e.CharacterId)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
        });

        modelBuilder.Entity<Buddy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("buddies");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.BuddyId)
                .HasColumnType("int(11)")
                .HasColumnName("buddyid");
            entity.Property(e => e.CharacterId)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
            entity.Property(e => e.Group)
                .HasMaxLength(17)
                .HasDefaultValueSql("'0'")
                .HasColumnName("group");
            entity.Property(e => e.Pending)
                .HasColumnType("tinyint(4)")
                .HasColumnName("pending");
        });

        modelBuilder.Entity<CharacterEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("characters");

            entity.HasIndex(e => e.AccountId, "accountid");

            entity.HasIndex(e => new { e.Id, e.AccountId, e.World }, "id");

            entity.HasIndex(e => new { e.Id, e.AccountId, e.Name }, "id_2");

            entity.HasIndex(e => e.Party, "party");

            entity.HasIndex(e => new { e.Level, e.Exp }, "ranking1");

            entity.HasIndex(e => new { e.Gm, e.JobId }, "ranking2");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.AccountId)
                .HasColumnType("int(11)")
                .HasColumnName("accountid");
            entity.Property(e => e.AllianceRank)
                .HasDefaultValueSql("'5'")
                .HasColumnType("int(10)")
                .HasColumnName("allianceRank");
            entity.Property(e => e.Ap)
                .HasColumnType("int(11)")
                .HasColumnName("ap");
            entity.Property(e => e.AriantPoints)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("ariantPoints");
            entity.Property(e => e.BuddyCapacity)
                .HasDefaultValueSql("'25'")
                .HasColumnType("int(11)")
                .HasColumnName("buddyCapacity");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("createdate");
            entity.Property(e => e.DataString)
                .HasMaxLength(64)
                .HasDefaultValueSql("''")
                .HasColumnName("dataString");
            entity.Property(e => e.Dex)
                .HasDefaultValueSql("'5'")
                .HasColumnType("int(11)")
                .HasColumnName("dex");
            entity.Property(e => e.DojoPoints)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("dojoPoints");
            entity.Property(e => e.Equipslots)
                .HasDefaultValueSql("'24'")
                .HasColumnType("int(11)")
                .HasColumnName("equipslots");
            entity.Property(e => e.Etcslots)
                .HasDefaultValueSql("'24'")
                .HasColumnType("int(11)")
                .HasColumnName("etcslots");
            entity.Property(e => e.Exp)
                .HasColumnType("int(11)")
                .HasColumnName("exp");
            entity.Property(e => e.Face)
                .HasColumnType("int(11)")
                .HasColumnName("face");
            entity.Property(e => e.Fame)
                .HasColumnType("int(11)")
                .HasColumnName("fame");
            entity.Property(e => e.FamilyId)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("int(11)")
                .HasColumnName("familyId");
            entity.Property(e => e.FinishedDojoTutorial)
                .HasColumnType("tinyint(1) unsigned")
                .HasColumnName("finishedDojoTutorial");
            entity.Property(e => e.Fquest)
                .HasColumnType("int(11)")
                .HasColumnName("fquest");
            entity.Property(e => e.Gachaexp)
                .HasColumnType("int(11)")
                .HasColumnName("gachaexp");
            entity.Property(e => e.Gender)
                .HasColumnType("int(11)")
                .HasColumnName("gender");
            entity.Property(e => e.Gm).HasColumnName("gm").HasColumnType("tinyint").HasDefaultValueSql("'0'");
            entity.Property(e => e.GuildId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("guildid");
            entity.Property(e => e.GuildRank)
                .HasDefaultValueSql("'5'")
                .HasColumnType("int(10) unsigned")
                .HasColumnName("guildrank");
            entity.Property(e => e.Hair)
                .HasColumnType("int(11)")
                .HasColumnName("hair");
            entity.Property(e => e.HasMerchant).HasDefaultValue(false);
            entity.Property(e => e.Hp)
                .HasDefaultValueSql("'50'")
                .HasColumnType("int(11)")
                .HasColumnName("hp");
            entity.Property(e => e.HpMpUsed)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("hpMpUsed");
            entity.Property(e => e.Int)
                .HasDefaultValueSql("'4'")
                .HasColumnType("int(11)")
                .HasColumnName("int");
            entity.Property(e => e.Jailexpire)
                .HasColumnType("bigint(20)")
                .HasColumnName("jailexpire");
            entity.Property(e => e.JobId)
                .HasColumnType("int(11)")
                .HasColumnName("job");
            entity.Property(e => e.JobRank)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(10) unsigned")
                .HasColumnName("jobRank");
            entity.Property(e => e.JobRankMove)
                .HasColumnType("int(11)")
                .HasColumnName("jobRankMove");
            entity.Property(e => e.LastDojoStage)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("lastDojoStage");
            entity.Property(e => e.LastExpGainTime)
                .HasDefaultValueSql("'2015-01-01 05:00:00'")
                .HasColumnType("timestamp")
                .HasColumnName("lastExpGainTime");
            entity.Property(e => e.LastLogoutTime)
                .HasDefaultValueSql("'2015-01-01 05:00:00'")
                .HasColumnType("timestamp")
                .HasColumnName("lastLogoutTime");
            entity.Property(e => e.Level)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("level");
            entity.Property(e => e.Luk)
                .HasDefaultValueSql("'4'")
                .HasColumnType("int(11)")
                .HasColumnName("luk");
            entity.Property(e => e.Map)
                .HasColumnType("int(11)")
                .HasColumnName("map");
            entity.Property(e => e.MarriageItemId)
                .HasColumnType("int(11)")
                .HasColumnName("marriageItemId");
            entity.Property(e => e.Matchcardlosses)
                .HasColumnType("int(11)")
                .HasColumnName("matchcardlosses");
            entity.Property(e => e.Matchcardties)
                .HasColumnType("int(11)")
                .HasColumnName("matchcardties");
            entity.Property(e => e.Matchcardwins)
                .HasColumnType("int(11)")
                .HasColumnName("matchcardwins");
            entity.Property(e => e.Maxhp)
                .HasDefaultValueSql("'50'")
                .HasColumnType("int(11)")
                .HasColumnName("maxhp");
            entity.Property(e => e.Maxmp)
                .HasDefaultValueSql("'5'")
                .HasColumnType("int(11)")
                .HasColumnName("maxmp");
            entity.Property(e => e.MerchantMesos)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)");
            entity.Property(e => e.Meso)
                .HasColumnType("int(11)")
                .HasColumnName("meso");
            entity.Property(e => e.MessengerId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("messengerid");
            entity.Property(e => e.MessengerPosition)
                .HasDefaultValueSql("'4'")
                .HasColumnType("int(10) unsigned")
                .HasColumnName("messengerposition");
            entity.Property(e => e.Monsterbookcover)
                .HasColumnType("int(11)")
                .HasColumnName("monsterbookcover");
            entity.Property(e => e.MountExp)
                .HasColumnType("int(9)")
                .HasColumnName("mountexp");
            entity.Property(e => e.MountLevel)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(9)")
                .HasColumnName("mountlevel");
            entity.Property(e => e.Mounttiredness)
                .HasColumnType("int(9)")
                .HasColumnName("mounttiredness");
            entity.Property(e => e.Mp)
                .HasDefaultValueSql("'5'")
                .HasColumnType("int(11)")
                .HasColumnName("mp");
            entity.Property(e => e.Name)
                .HasMaxLength(13)
                .HasDefaultValueSql("''")
                .HasColumnName("name");
            entity.Property(e => e.Omoklosses)
                .HasColumnType("int(11)")
                .HasColumnName("omoklosses");
            entity.Property(e => e.Omokties)
                .HasColumnType("int(11)")
                .HasColumnName("omokties");
            entity.Property(e => e.Omokwins)
                .HasColumnType("int(11)")
                .HasColumnName("omokwins");
            entity.Property(e => e.PartnerId)
                .HasColumnType("int(11)")
                .HasColumnName("partnerId");
            entity.Property(e => e.Party)
                .HasColumnType("int(11)")
                .HasColumnName("party");
            entity.Property(e => e.PartySearch)
                .IsRequired()
                .HasDefaultValue(true)
                .HasSentinel(true)
                .HasColumnName("partySearch");
            entity.Property(e => e.Pqpoints)
                .HasColumnType("int(11)")
                .HasColumnName("PQPoints");
            entity.Property(e => e.Rank)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(10) unsigned")
                .HasColumnName("rank");
            entity.Property(e => e.RankMove)
                .HasColumnType("int(11)")
                .HasColumnName("rankMove");
            entity.Property(e => e.Reborns)
                .HasColumnType("int(5)")
                .HasColumnName("reborns");
            entity.Property(e => e.Setupslots)
                .HasDefaultValueSql("'24'")
                .HasColumnType("int(11)")
                .HasColumnName("setupslots");
            entity.Property(e => e.Skincolor)
                .HasColumnType("int(11)")
                .HasColumnName("skincolor");
            entity.Property(e => e.Sp)
                .HasMaxLength(128)
                .HasDefaultValueSql("'0,0,0,0,0,0,0,0,0,0'")
                .HasColumnName("sp");
            entity.Property(e => e.Spawnpoint)
                .HasColumnType("int(11)")
                .HasColumnName("spawnpoint");
            entity.Property(e => e.Str)
                .HasDefaultValueSql("'12'")
                .HasColumnType("int(11)")
                .HasColumnName("str");
            entity.Property(e => e.SummonValue)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("summonValue");
            entity.Property(e => e.Useslots)
                .HasDefaultValueSql("'24'")
                .HasColumnType("int(11)")
                .HasColumnName("useslots");
            entity.Property(e => e.VanquisherKills)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("vanquisherKills");
            entity.Property(e => e.VanquisherStage)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("vanquisherStage");
            entity.Property(e => e.World)
                .HasColumnType("int(11)")
                .HasColumnName("world");
        });

        modelBuilder.Entity<Cooldown>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("cooldowns");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Charid)
                .HasColumnType("int(11)")
                .HasColumnName("charid");
            entity.Property(e => e.Length)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("length");
            entity.Property(e => e.SkillId)
                .HasColumnType("int(11)")
                .HasColumnName("SkillID");
            entity.Property(e => e.StartTime).HasColumnType("bigint(20) unsigned");
        });

        modelBuilder.Entity<DropDataGlobal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("drop_data_global");

            entity.HasIndex(e => e.Continent, "mobid");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20)")
                .HasColumnName("id");
            entity.Property(e => e.Chance)
                .HasColumnType("int(11)")
                .HasColumnName("chance");
            entity.Property(e => e.Comments)
                .HasMaxLength(45)
                .HasColumnName("comments");
            entity.Property(e => e.Continent)
                .IsRequired()
                .HasDefaultValueSql("'-1'")
                .HasColumnName("continent")
                .HasColumnType("tinyint");
            entity.Property(e => e.Itemid)
                .HasColumnType("int(11)")
                .HasColumnName("itemid");
            entity.Property(e => e.MaximumQuantity)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("maximum_quantity");
            entity.Property(e => e.MinimumQuantity)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("minimum_quantity");
            entity.Property(e => e.Questid)
                .HasColumnType("int(11)")
                .HasColumnName("questid");
        });

        modelBuilder.Entity<DropData>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("drop_data");

            entity.HasIndex(e => new { e.Dropperid, e.Itemid }, "dropperid").IsUnique();

            entity.HasIndex(e => new { e.Dropperid, e.Itemid }, "dropperid_2");

            entity.HasIndex(e => e.Dropperid, "mobid");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20)")
                .HasColumnName("id");
            entity.Property(e => e.Chance)
                .HasColumnType("int(11)")
                .HasColumnName("chance");
            entity.Property(e => e.Dropperid)
                .HasColumnType("int(11)")
                .HasColumnName("dropperid");
            entity.Property(e => e.Itemid)
                .HasColumnType("int(11)")
                .HasColumnName("itemid");
            entity.Property(e => e.MaximumQuantity)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("maximum_quantity");
            entity.Property(e => e.MinimumQuantity)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("minimum_quantity");
            entity.Property(e => e.Questid)
                .HasColumnType("int(11)")
                .HasColumnName("questid");
        });

        modelBuilder.Entity<Dueyitem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("dueyitems");

            entity.HasIndex(e => e.Inventoryitemid, "INVENTORYITEMID");

            entity.HasIndex(e => e.PackageId, "PackageId");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Inventoryitemid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("inventoryitemid");
            entity.Property(e => e.PackageId).HasColumnType("int(10) unsigned");

            entity.HasOne(d => d.Package).WithMany(p => p.Dueyitems)
                .HasForeignKey(d => d.PackageId)
                .HasConstraintName("dueyitems_ibfk_1");
        });

        modelBuilder.Entity<Dueypackage>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("PRIMARY");

            entity.ToTable("dueypackages");

            entity.Property(e => e.PackageId).HasColumnType("int(10) unsigned");
            entity.Property(e => e.Checked)
                .HasDefaultValue(true)
                .HasSentinel(true)
                .HasColumnType("tinyint(1) unsigned");
            entity.Property(e => e.Mesos)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(10) unsigned");
            entity.Property(e => e.Message).HasMaxLength(200);
            entity.Property(e => e.ReceiverId).HasColumnType("int(10) unsigned");
            entity.Property(e => e.SenderName).HasMaxLength(13);
            entity.Property(e => e.TimeStamp)
                .HasDefaultValueSql("'2015-01-01 05:00:00'")
                .HasColumnType("timestamp");
            entity.Property(e => e.Type)
                .HasDefaultValue(false)
                .HasColumnType("tinyint(1) unsigned");
            entity.Ignore(e => e.Item);
        });

        modelBuilder.Entity<Eventstat>(entity =>
        {
            entity.HasKey(e => e.Characterid).HasName("PRIMARY");

            entity.ToTable("eventstats");

            entity.Property(e => e.Characterid)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("characterid");
            entity.Property(e => e.Info)
                .HasColumnType("int(11)")
                .HasColumnName("info");
            entity.Property(e => e.Name)
                .HasMaxLength(11)
                .HasDefaultValueSql("'0'")
                .HasComment("0")
                .HasColumnName("name");
        });

        modelBuilder.Entity<Famelog>(entity =>
        {
            entity.HasKey(e => e.Famelogid).HasName("PRIMARY");

            entity.ToTable("famelog");

            entity.HasIndex(e => e.Characterid, "characterid");

            entity.Property(e => e.Famelogid)
                .HasColumnType("int(11)")
                .HasColumnName("famelogid");
            entity.Property(e => e.Characterid)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
            entity.Property(e => e.CharacteridTo)
                .HasColumnType("int(11)")
                .HasColumnName("characterid_to");
            entity.Property(e => e.When)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("when");

            entity.HasOne(d => d.Character).WithMany(p => p.Famelogs)
                .HasForeignKey(d => d.Characterid)
                .HasConstraintName("famelog_ibfk_1");
        });

        modelBuilder.Entity<FamilyCharacter>(entity =>
        {
            entity.HasKey(e => e.Cid).HasName("PRIMARY");

            entity.ToTable("family_character");

            entity.HasIndex(e => new { e.Cid, e.Familyid }, "cid");

            entity.Property(e => e.Cid)
                .HasColumnType("int(11)")
                .HasColumnName("cid");
            entity.Property(e => e.Familyid)
                .HasColumnType("int(11)")
                .HasColumnName("familyid");
            entity.Property(e => e.Lastresettime)
                .HasColumnType("bigint(20)")
                .HasColumnName("lastresettime");
            entity.Property(e => e.Precepts)
                .HasMaxLength(200)
                .HasColumnName("precepts");
            entity.Property(e => e.Reptosenior)
                .HasColumnType("int(11)")
                .HasColumnName("reptosenior");
            entity.Property(e => e.Reputation)
                .HasColumnType("int(11)")
                .HasColumnName("reputation");
            entity.Property(e => e.Seniorid)
                .HasColumnType("int(11)")
                .HasColumnName("seniorid");
            entity.Property(e => e.Todaysrep)
                .HasColumnType("int(11)")
                .HasColumnName("todaysrep");
            entity.Property(e => e.Totalreputation)
                .HasColumnType("int(11)")
                .HasColumnName("totalreputation");

            entity.HasOne(d => d.CidNavigation).WithOne(p => p.FamilyCharacter)
                .HasForeignKey<FamilyCharacter>(d => d.Cid)
                .HasConstraintName("family_character_ibfk_1");
        });

        modelBuilder.Entity<DB_FamilyEntitlement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("family_entitlement");

            entity.HasIndex(e => e.Charid, "charid");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Charid)
                .HasColumnType("int(11)")
                .HasColumnName("charid");
            entity.Property(e => e.Entitlementid)
                .HasColumnType("int(11)")
                .HasColumnName("entitlementid");
            entity.Property(e => e.Timestamp)
                .HasColumnType("bigint(20)")
                .HasColumnName("timestamp");
        });

        modelBuilder.Entity<Fredstorage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("fredstorage");

            entity.HasIndex(e => e.Cid, "cid_2").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Cid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("cid");
            entity.Property(e => e.Daynotes)
                .HasColumnType("int(4) unsigned")
                .HasColumnName("daynotes");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("timestamp");
        });

        modelBuilder.Entity<Gift>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("gifts");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.From)
                .HasMaxLength(13)
                .HasColumnName("from");
            entity.Property(e => e.Message)
                .HasColumnType("tinytext")
                .HasColumnName("message");
            entity.Property(e => e.Ringid)
                .HasColumnType("int(10)")
                .HasColumnName("ringid");
            entity.Property(e => e.Sn)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("sn");
            entity.Property(e => e.To)
                .HasColumnType("int(11)")
                .HasColumnName("to");
        });

        modelBuilder.Entity<GuildEntity>(entity =>
        {
            entity.HasKey(e => e.GuildId).HasName("PRIMARY");

            entity.ToTable("guilds");

            entity.HasIndex(e => new { e.GuildId, e.Name }, "guildid");

            entity.Property(e => e.GuildId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("guildid");
            entity.Property(e => e.AllianceId)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("allianceId");
            entity.Property(e => e.Capacity)
                .HasDefaultValueSql("'10'")
                .HasColumnType("int(10) unsigned")
                .HasColumnName("capacity");
            entity.Property(e => e.GP)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("GP");
            entity.Property(e => e.Leader)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("leader");
            entity.Property(e => e.Logo)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("logo");
            entity.Property(e => e.LogoBg)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("logoBG");
            entity.Property(e => e.LogoBgColor)
                .HasColumnType("smallint(5) unsigned")
                .HasColumnName("logoBGColor");
            entity.Property(e => e.LogoColor)
                .HasColumnType("smallint(5) unsigned")
                .HasColumnName("logoColor");
            entity.Property(e => e.Name)
                .HasMaxLength(45)
                .HasColumnName("name");
            entity.Property(e => e.Notice)
                .HasMaxLength(101)
                .HasColumnName("notice");
            entity.Property(e => e.Rank1Title)
                .HasMaxLength(45)
                .HasDefaultValueSql("'Master'")
                .HasColumnName("rank1title");
            entity.Property(e => e.Rank2Title)
                .HasMaxLength(45)
                .HasDefaultValueSql("'Jr. Master'")
                .HasColumnName("rank2title");
            entity.Property(e => e.Rank3Title)
                .HasMaxLength(45)
                .HasDefaultValueSql("'Member'")
                .HasColumnName("rank3title");
            entity.Property(e => e.Rank4Title)
                .HasMaxLength(45)
                .HasDefaultValueSql("'Member'")
                .HasColumnName("rank4title");
            entity.Property(e => e.Rank5Title)
                .HasMaxLength(45)
                .HasDefaultValueSql("'Member'")
                .HasColumnName("rank5title");
            entity.Property(e => e.Signature)
                .HasColumnType("int(11)")
                .HasColumnName("signature");
        });

        modelBuilder.Entity<Hwidaccount>(entity =>
        {
            entity.HasKey(e => new { e.AccountId, e.Hwid }).HasName("PRIMARY");

            entity.ToTable("hwidaccounts");

            entity.Property(e => e.AccountId)
                .HasColumnType("int(11)")
                .HasColumnName("accountid");
            entity.Property(e => e.Hwid)
                .HasMaxLength(40)
                .HasDefaultValueSql("''")
                .HasColumnName("hwid");
            entity.Property(e => e.ExpiresAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("expiresat");
            entity.Property(e => e.Relevance)
                .HasColumnType("tinyint(2)")
                .HasColumnName("relevance");
        });

        modelBuilder.Entity<Hwidban>(entity =>
        {
            entity.HasKey(e => e.Hwidbanid).HasName("PRIMARY");

            entity.ToTable("hwidbans");

            entity.HasIndex(e => e.Hwid, "hwid_2").IsUnique();

            entity.Property(e => e.Hwidbanid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("hwidbanid");
            entity.Property(e => e.Hwid)
                .HasMaxLength(30)
                .HasColumnName("hwid");
        });

        modelBuilder.Entity<Inventoryequipment>(entity =>
        {
            entity.HasKey(e => e.Inventoryequipmentid).HasName("PRIMARY");

            entity.ToTable("inventoryequipment");

            entity.HasIndex(e => e.Inventoryitemid, "INVENTORYITEMID");

            entity.Property(e => e.Inventoryequipmentid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("inventoryequipmentid");
            entity.Property(e => e.Acc)
                .HasColumnType("int(11)")
                .HasColumnName("acc");
            entity.Property(e => e.Avoid)
                .HasColumnType("int(11)")
                .HasColumnName("avoid");
            entity.Property(e => e.Dex)
                .HasColumnType("int(11)")
                .HasColumnName("dex");
            entity.Property(e => e.Hands)
                .HasColumnType("int(11)")
                .HasColumnName("hands");
            entity.Property(e => e.Hp)
                .HasColumnType("int(11)")
                .HasColumnName("hp");
            entity.Property(e => e.Int)
                .HasColumnType("int(11)")
                .HasColumnName("int");
            entity.Property(e => e.Inventoryitemid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("inventoryitemid");
            entity.Property(e => e.Itemexp)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("itemexp");
            entity.Property(e => e.Itemlevel)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("itemlevel");
            entity.Property(e => e.Jump)
                .HasColumnType("int(11)")
                .HasColumnName("jump");
            entity.Property(e => e.Level)
                .HasColumnType("int(11)")
                .HasColumnName("level");
            entity.Property(e => e.Locked)
                .HasColumnType("int(11)")
                .HasColumnName("locked");
            entity.Property(e => e.Luk)
                .HasColumnType("int(11)")
                .HasColumnName("luk");
            entity.Property(e => e.Matk)
                .HasColumnType("int(11)")
                .HasColumnName("matk");
            entity.Property(e => e.Mdef)
                .HasColumnType("int(11)")
                .HasColumnName("mdef");
            entity.Property(e => e.Mp)
                .HasColumnType("int(11)")
                .HasColumnName("mp");
            entity.Property(e => e.RingId)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("int(11)")
                .HasColumnName("ringid");
            entity.Property(e => e.Speed)
                .HasColumnType("int(11)")
                .HasColumnName("speed");
            entity.Property(e => e.Str)
                .HasColumnType("int(11)")
                .HasColumnName("str");
            entity.Property(e => e.Upgradeslots)
                .HasColumnType("int(11)")
                .HasColumnName("upgradeslots");
            entity.Property(e => e.Vicious)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("vicious");
            entity.Property(e => e.Watk)
                .HasColumnType("int(11)")
                .HasColumnName("watk");
            entity.Property(e => e.Wdef)
                .HasColumnType("int(11)")
                .HasColumnName("wdef");
        });

        modelBuilder.Entity<Inventoryitem>(entity =>
        {
            entity.HasKey(e => e.Inventoryitemid).HasName("PRIMARY");

            entity.ToTable("inventoryitems");

            entity.HasIndex(e => e.Characterid, "CHARID");

            entity.Property(e => e.Inventoryitemid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("inventoryitemid");
            entity.Property(e => e.Accountid)
                .HasColumnType("int(11)")
                .HasColumnName("accountid");
            entity.Property(e => e.Characterid)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
            entity.Property(e => e.Expiration)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("bigint(20)")
                .HasColumnName("expiration");
            entity.Property(e => e.Flag)
                .HasColumnType("int(11)")
                .HasColumnName("flag");
            entity.Property(e => e.GiftFrom)
                .HasMaxLength(26)
                .HasColumnName("giftFrom");
            entity.Property(e => e.Inventorytype)
                .HasColumnType("int(11)")
                .HasColumnName("inventorytype");
            entity.Property(e => e.Itemid)
                .HasColumnType("int(11)")
                .HasColumnName("itemid");
            entity.Property(e => e.Owner)
                .HasColumnType("tinytext")
                .HasColumnName("owner");
            entity.Property(e => e.Petid)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("int(11)")
                .HasColumnName("petid");
            entity.Property(e => e.Position)
                .HasColumnType("int(11)")
                .HasColumnName("position");
            entity.Property(e => e.Quantity)
                .HasColumnType("int(11)")
                .HasColumnName("quantity");
            entity.Property(e => e.Type)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("type");
        });

        modelBuilder.Entity<Inventorymerchant>(entity =>
        {
            entity.HasKey(e => e.Inventorymerchantid).HasName("PRIMARY");

            entity.ToTable("inventorymerchant");

            entity.HasIndex(e => e.Inventoryitemid, "INVENTORYITEMID");

            entity.Property(e => e.Inventorymerchantid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("inventorymerchantid");
            entity.Property(e => e.Bundles)
                .HasColumnType("int(10)")
                .HasColumnName("bundles");
            entity.Property(e => e.Characterid)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
            entity.Property(e => e.Inventoryitemid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("inventoryitemid");
        });

        modelBuilder.Entity<Ipban>(entity =>
        {
            entity.HasKey(e => e.Ipbanid).HasName("PRIMARY");

            entity.ToTable("ipbans");

            entity.Property(e => e.Ipbanid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("ipbanid");
            entity.Property(e => e.Aid)
                .HasMaxLength(40)
                .HasColumnName("aid");
            entity.Property(e => e.Ip)
                .HasMaxLength(40)
                .HasDefaultValueSql("''")
                .HasColumnName("ip");
        });

        modelBuilder.Entity<Keymap>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("keymap");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Action)
                .HasColumnType("int(11)")
                .HasColumnName("action");
            entity.Property(e => e.Characterid)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
            entity.Property(e => e.Key)
                .HasColumnType("int(11)")
                .HasColumnName("key");
            entity.Property(e => e.Type)
                .HasColumnType("int(11)")
                .HasColumnName("type");
        });

        modelBuilder.Entity<Macban>(entity =>
        {
            entity.HasKey(e => e.Macbanid).HasName("PRIMARY");

            entity.ToTable("macbans");

            entity.HasIndex(e => e.Mac, "mac_2").IsUnique();

            entity.Property(e => e.Macbanid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("macbanid");
            entity.Property(e => e.Aid)
                .HasMaxLength(40)
                .HasColumnName("aid");
            entity.Property(e => e.Mac)
                .HasMaxLength(30)
                .HasColumnName("mac");
        });

        modelBuilder.Entity<Macfilter>(entity =>
        {
            entity.HasKey(e => e.Macfilterid).HasName("PRIMARY");

            entity.ToTable("macfilters");

            entity.Property(e => e.Macfilterid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("macfilterid");
            entity.Property(e => e.Filter)
                .HasMaxLength(30)
                .HasColumnName("filter");
        });

        modelBuilder.Entity<Makercreatedatum>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.Itemid }).HasName("PRIMARY");

            entity.ToTable("makercreatedata");

            entity.Property(e => e.Id)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Itemid)
                .HasColumnType("int(11)")
                .HasColumnName("itemid");
            entity.Property(e => e.Catalyst)
                .HasColumnType("int(11)")
                .HasColumnName("catalyst");
            entity.Property(e => e.Quantity)
                .HasColumnType("smallint(6)")
                .HasColumnName("quantity");
            entity.Property(e => e.ReqEquip)
                .HasColumnType("int(11)")
                .HasColumnName("req_equip");
            entity.Property(e => e.ReqItem)
                .HasColumnType("int(11)")
                .HasColumnName("req_item");
            entity.Property(e => e.ReqLevel)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("req_level");
            entity.Property(e => e.ReqMakerLevel)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("req_maker_level");
            entity.Property(e => e.ReqMeso)
                .HasColumnType("int(11)")
                .HasColumnName("req_meso");
            entity.Property(e => e.Tuc)
                .HasColumnType("tinyint(3)")
                .HasColumnName("tuc");
        });

        modelBuilder.Entity<Makerreagentdatum>(entity =>
        {
            entity.HasKey(e => e.Itemid).HasName("PRIMARY");

            entity.ToTable("makerreagentdata");

            entity.Property(e => e.Itemid)
                .HasColumnType("int(11)")
                .HasColumnName("itemid");
            entity.Property(e => e.Stat)
                .HasMaxLength(20)
                .HasColumnName("stat");
            entity.Property(e => e.Value)
                .HasColumnType("smallint(6)")
                .HasColumnName("value");
        });

        modelBuilder.Entity<Makerrecipedatum>(entity =>
        {
            entity.HasKey(e => new { e.Itemid, e.ReqItem }).HasName("PRIMARY");

            entity.ToTable("makerrecipedata");

            entity.Property(e => e.Itemid)
                .HasColumnType("int(11)")
                .HasColumnName("itemid");
            entity.Property(e => e.ReqItem)
                .HasColumnType("int(11)")
                .HasColumnName("req_item");
            entity.Property(e => e.Count)
                .HasColumnType("smallint(6)")
                .HasColumnName("count");
        });

        modelBuilder.Entity<Makerrewarddatum>(entity =>
        {
            entity.HasKey(e => new { e.Itemid, e.Rewardid }).HasName("PRIMARY");

            entity.ToTable("makerrewarddata");

            entity.Property(e => e.Itemid)
                .HasColumnType("int(11)")
                .HasColumnName("itemid");
            entity.Property(e => e.Rewardid)
                .HasColumnType("int(11)")
                .HasColumnName("rewardid");
            entity.Property(e => e.Prob)
                .HasDefaultValueSql("'100'")
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("prob");
            entity.Property(e => e.Quantity)
                .HasColumnType("smallint(6)")
                .HasColumnName("quantity");
        });

        modelBuilder.Entity<DB_Marriage>(entity =>
        {
            entity.HasKey(e => e.Marriageid).HasName("PRIMARY");

            entity.ToTable("marriages");

            entity.Property(e => e.Marriageid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("marriageid");
            entity.Property(e => e.Husbandid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("husbandid");
            entity.Property(e => e.Wifeid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("wifeid");
        });

        modelBuilder.Entity<Medalmap>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("medalmaps");

            entity.HasIndex(e => e.Queststatusid, "queststatusid");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Characterid)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
            entity.Property(e => e.Mapid)
                .HasColumnType("int(11)")
                .HasColumnName("mapid");
            entity.Property(e => e.Queststatusid)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("queststatusid");
        });

        modelBuilder.Entity<Monsterbook>(entity =>
        {
            entity.HasKey(e => new { e.Cardid, e.Charid });
            entity
                .ToTable("monsterbook");

            entity.Property(e => e.Cardid)
                .HasColumnType("int(11)")
                .HasColumnName("cardid");
            entity.Property(e => e.Charid)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("charid");
            entity.Property(e => e.Level)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(1)")
                .HasColumnName("level");
        });

        modelBuilder.Entity<Monstercarddatum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("monstercarddata");

            entity.HasIndex(e => e.Id, "id").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Cardid)
                .HasColumnType("int(11)")
                .HasColumnName("cardid");
            entity.Property(e => e.Mobid)
                .HasColumnType("int(11)")
                .HasColumnName("mobid");
        });

        modelBuilder.Entity<MtsCart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("mts_cart");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Cid)
                .HasColumnType("int(11)")
                .HasColumnName("cid");
            entity.Property(e => e.Itemid)
                .HasColumnType("int(11)")
                .HasColumnName("itemid");
        });

        modelBuilder.Entity<MtsItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("mts_items");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Acc)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("acc");
            entity.Property(e => e.Avoid)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("avoid");
            entity.Property(e => e.BidIncre)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("bid_incre");
            entity.Property(e => e.BuyNow)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("buy_now");
            entity.Property(e => e.Dex)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("dex");
            entity.Property(e => e.Expiration)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("bigint(20)")
                .HasColumnName("expiration");
            entity.Property(e => e.Flag)
                .HasColumnType("int(2) unsigned")
                .HasColumnName("flag");
            entity.Property(e => e.GiftFrom)
                .HasMaxLength(26)
                .HasColumnName("giftFrom");
            entity.Property(e => e.Hands)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("hands");
            entity.Property(e => e.Hp)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("hp");
            entity.Property(e => e.Int)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("int");
            entity.Property(e => e.Isequip)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(1)")
                .HasColumnName("isequip");
            entity.Property(e => e.Itemexp)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("itemexp");
            entity.Property(e => e.Itemid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("itemid");
            entity.Property(e => e.Itemlevel)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("itemlevel");
            entity.Property(e => e.Jump)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("jump");
            entity.Property(e => e.Level)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("level");
            entity.Property(e => e.Locked)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("locked");
            entity.Property(e => e.Luk)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("luk");
            entity.Property(e => e.Matk)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("matk");
            entity.Property(e => e.Mdef)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("mdef");
            entity.Property(e => e.Mp)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("mp");
            entity.Property(e => e.Owner)
                .HasMaxLength(16)
                .HasDefaultValueSql("''")
                .HasColumnName("owner");
            entity.Property(e => e.Position)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("position");
            entity.Property(e => e.Price)
                .HasColumnType("int(11)")
                .HasColumnName("price");
            entity.Property(e => e.Quantity)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("quantity");
            entity.Property(e => e.Ringid)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("int(11)")
                .HasColumnName("ringid");
            entity.Property(e => e.SellEnds)
                .HasMaxLength(16)
                .HasColumnName("sell_ends");
            entity.Property(e => e.Seller)
                .HasColumnType("int(11)")
                .HasColumnName("seller");
            entity.Property(e => e.Sellername)
                .HasMaxLength(16)
                .HasColumnName("sellername");
            entity.Property(e => e.Speed)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("speed");
            entity.Property(e => e.Str)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("str");
            entity.Property(e => e.Tab)
                .HasColumnType("int(11)")
                .HasColumnName("tab");
            entity.Property(e => e.Transfer)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(2)")
                .HasColumnName("transfer");
            entity.Property(e => e.Type)
                .HasColumnType("int(11)")
                .HasColumnName("type");
            entity.Property(e => e.Upgradeslots)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("upgradeslots");
            entity.Property(e => e.Vicious)
                .HasColumnType("int(2) unsigned")
                .HasColumnName("vicious");
            entity.Property(e => e.Watk)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("watk");
            entity.Property(e => e.Wdef)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("wdef");
        });

        modelBuilder.Entity<Namechange>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("namechanges");

            entity.HasIndex(e => e.Characterid, "characterid");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Characterid)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
            entity.Property(e => e.CompletionTime)
                .HasColumnType("timestamp")
                .HasColumnName("completionTime");
            entity.Property(e => e.New)
                .HasMaxLength(13)
                .HasColumnName("new");
            entity.Property(e => e.Old)
                .HasMaxLength(13)
                .HasColumnName("old");
            entity.Property(e => e.RequestTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("requestTime");
        });

        modelBuilder.Entity<Newyear>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("newyear");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Message)
                .HasMaxLength(120)
                .HasDefaultValueSql("''")
                .HasColumnName("message");
            entity.Property(e => e.Received).HasColumnName("received");
            entity.Property(e => e.ReceiverDiscard).HasColumnName("receiverdiscard");
            entity.Property(e => e.ReceiverId)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("int(10)")
                .HasColumnName("receiverid");
            entity.Property(e => e.ReceiverName)
                .HasMaxLength(13)
                .HasDefaultValueSql("''")
                .HasColumnName("receivername");
            entity.Property(e => e.SenderDiscard).HasColumnName("senderdiscard");
            entity.Property(e => e.SenderId)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("int(10)")
                .HasColumnName("senderid");
            entity.Property(e => e.SenderName)
                .HasMaxLength(13)
                .HasDefaultValueSql("''")
                .HasColumnName("sendername");
            entity.Property(e => e.TimeReceived)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("timereceived");
            entity.Property(e => e.TimeSent)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("timesent");
        });

        modelBuilder.Entity<DB_Note>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("notes");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Deleted)
                .HasColumnType("int(2)")
                .HasColumnName("deleted");
            entity.Property(e => e.Fame)
                .HasColumnType("int(11)")
                .HasColumnName("fame");
            entity.Property(e => e.From)
                .HasMaxLength(13)
                .HasDefaultValueSql("''")
                .HasColumnName("from");
            entity.Property(e => e.Message)
                .HasColumnType("text")
                .HasColumnName("message");
            entity.Property(e => e.Timestamp)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("timestamp");
            entity.Property(e => e.To)
                .HasMaxLength(13)
                .HasDefaultValueSql("''")
                .HasColumnName("to");
        });

        modelBuilder.Entity<Nxcode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("nxcode");

            entity.HasIndex(e => e.Code, "code").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(17)
                .HasColumnName("code");
            entity.Property(e => e.Expiration)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("expiration");
            entity.Property(e => e.Retriever)
                .HasMaxLength(13)
                .HasColumnName("retriever");
        });

        modelBuilder.Entity<NxcodeItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("nxcode_items");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Codeid)
                .HasColumnType("int(11)")
                .HasColumnName("codeid");
            entity.Property(e => e.Item)
                .HasDefaultValueSql("'4000000'")
                .HasColumnType("int(11)")
                .HasColumnName("item");
            entity.Property(e => e.Quantity)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("quantity");
            entity.Property(e => e.Type)
                .HasDefaultValueSql("'5'")
                .HasColumnType("int(11)")
                .HasColumnName("type");
        });

        modelBuilder.Entity<Nxcoupon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("nxcoupons");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Activeday)
                .HasColumnType("int(11)")
                .HasColumnName("activeday");
            entity.Property(e => e.CouponId)
                .HasColumnType("int(11)")
                .HasColumnName("couponid");
            entity.Property(e => e.Endhour)
                .HasColumnType("int(11)")
                .HasColumnName("endhour");
            entity.Property(e => e.Rate)
                .HasColumnType("int(11)")
                .HasColumnName("rate");
            entity.Property(e => e.Starthour)
                .HasColumnType("int(11)")
                .HasColumnName("starthour");
        });

        modelBuilder.Entity<PetEntity>(entity =>
        {
            entity.HasKey(e => e.Petid).HasName("PRIMARY");

            entity.ToTable("pets");

            entity.Property(e => e.Petid)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("petid");
            entity.Property(e => e.Closeness)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("closeness");
            entity.Property(e => e.Flag)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("flag");
            entity.Property(e => e.Fullness)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("fullness");
            entity.Property(e => e.Level)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("level");
            entity.Property(e => e.Name)
                .HasMaxLength(13)
                .HasColumnName("name");
            entity.Property(e => e.Summoned).HasColumnName("summoned");
        });

        modelBuilder.Entity<Petignore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("petignores");

            entity.HasIndex(e => e.Petid, "fk_petignorepetid");

            entity.Property(e => e.Id)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Itemid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("itemid");
            entity.Property(e => e.Petid)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("petid");

            entity.HasOne(d => d.Pet).WithMany(p => p.Petignores)
                .HasForeignKey(d => d.Petid)
                .HasConstraintName("fk_petignorepetid");
        });

        modelBuilder.Entity<Playerdisease>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("playerdiseases");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Charid)
                .HasColumnType("int(11)")
                .HasColumnName("charid");
            entity.Property(e => e.Disease)
                .HasColumnType("int(11)")
                .HasColumnName("disease");
            entity.Property(e => e.Length)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("length");
            entity.Property(e => e.Mobskillid)
                .HasColumnType("int(11)")
                .HasColumnName("mobskillid");
            entity.Property(e => e.Mobskilllv)
                .HasColumnType("int(11)")
                .HasColumnName("mobskilllv");
        });

        modelBuilder.Entity<Playernpc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("playernpcs");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Cy)
                .HasColumnType("int(11)")
                .HasColumnName("cy");
            entity.Property(e => e.Dir)
                .HasColumnType("int(11)")
                .HasColumnName("dir");
            entity.Property(e => e.Face)
                .HasColumnType("int(11)")
                .HasColumnName("face");
            entity.Property(e => e.Fh)
                .HasColumnType("int(11)")
                .HasColumnName("fh");
            entity.Property(e => e.Gender)
                .HasColumnType("int(11)")
                .HasColumnName("gender");
            entity.Property(e => e.Hair)
                .HasColumnType("int(11)")
                .HasColumnName("hair");
            entity.Property(e => e.Job)
                .HasColumnType("int(11)")
                .HasColumnName("job");
            entity.Property(e => e.Map)
                .HasColumnType("int(11)")
                .HasColumnName("map");
            entity.Property(e => e.Name)
                .HasMaxLength(13)
                .HasColumnName("name");
            entity.Property(e => e.Overallrank)
                .HasColumnType("int(11)")
                .HasColumnName("overallrank");
            entity.Property(e => e.Rx0)
                .HasColumnType("int(11)")
                .HasColumnName("rx0");
            entity.Property(e => e.Rx1)
                .HasColumnType("int(11)")
                .HasColumnName("rx1");
            entity.Property(e => e.Scriptid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("scriptid");
            entity.Property(e => e.Skin)
                .HasColumnType("int(11)")
                .HasColumnName("skin");
            entity.Property(e => e.World)
                .HasColumnType("int(11)")
                .HasColumnName("world");
            entity.Property(e => e.Worldjobrank)
                .HasColumnType("int(11)")
                .HasColumnName("worldjobrank");
            entity.Property(e => e.Worldrank)
                .HasColumnType("int(11)")
                .HasColumnName("worldrank");
            entity.Property(e => e.X)
                .HasColumnType("int(11)")
                .HasColumnName("x");
        });

        modelBuilder.Entity<PlayernpcsEquip>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("playernpcs_equip");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Equipid)
                .HasColumnType("int(11)")
                .HasColumnName("equipid");
            entity.Property(e => e.Equippos)
                .HasColumnType("int(11)")
                .HasColumnName("equippos");
            entity.Property(e => e.Npcid)
                .HasColumnType("int(11)")
                .HasColumnName("npcid");
            entity.Property(e => e.Type)
                .HasColumnType("int(11)")
                .HasColumnName("type");
        });

        modelBuilder.Entity<PlayernpcsField>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("playernpcs_field");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Map)
                .HasColumnType("int(11)")
                .HasColumnName("map");
            entity.Property(e => e.Podium)
                .HasColumnType("smallint(8)")
                .HasColumnName("podium");
            entity.Property(e => e.Step).HasColumnName("step").HasColumnType("tinyint").HasDefaultValueSql("'0'");
            entity.Property(e => e.World)
                .HasColumnType("int(11)")
                .HasColumnName("world");
        });

        modelBuilder.Entity<Plife>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("plife");

            entity.Property(e => e.Id)
                .HasColumnType("int(11) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Cy)
                .HasColumnType("int(11)")
                .HasColumnName("cy");
            entity.Property(e => e.F)
                .HasColumnType("int(11)")
                .HasColumnName("f");
            entity.Property(e => e.Fh)
                .HasColumnType("int(11)")
                .HasColumnName("fh");
            entity.Property(e => e.Hide)
                .HasColumnType("int(11)")
                .HasColumnName("hide");
            entity.Property(e => e.Life)
                .HasColumnType("int(11)")
                .HasColumnName("life");
            entity.Property(e => e.Map)
                .HasColumnType("int(11)")
                .HasColumnName("map");
            entity.Property(e => e.Mobtime)
                .HasColumnType("int(11)")
                .HasColumnName("mobtime");
            entity.Property(e => e.Rx0)
                .HasColumnType("int(11)")
                .HasColumnName("rx0");
            entity.Property(e => e.Rx1)
                .HasColumnType("int(11)")
                .HasColumnName("rx1");
            entity.Property(e => e.Team)
                .HasColumnType("int(11)")
                .HasColumnName("team");
            entity.Property(e => e.Type)
                .HasMaxLength(1)
                .HasDefaultValueSql("'n'")
                .HasColumnName("type");
            entity.Property(e => e.World)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("int(11)")
                .HasColumnName("world");
            entity.Property(e => e.X)
                .HasColumnType("int(11)")
                .HasColumnName("x");
            entity.Property(e => e.Y)
                .HasColumnType("int(11)")
                .HasColumnName("y");
        });

        modelBuilder.Entity<Questprogress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("questprogress");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Characterid)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
            entity.Property(e => e.Progress)
                .HasMaxLength(15)
                .HasDefaultValueSql("''")
                .HasColumnName("progress");
            entity.Property(e => e.Progressid)
                .HasColumnType("int(11)")
                .HasColumnName("progressid");
            entity.Property(e => e.Queststatusid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("queststatusid");
        });

        modelBuilder.Entity<QuestStatusEntity>(entity =>
        {
            entity.HasKey(e => e.Queststatusid).HasName("PRIMARY");

            entity.ToTable("queststatus");

            entity.Property(e => e.Queststatusid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("queststatusid");
            entity.Property(e => e.Characterid)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
            entity.Property(e => e.Completed)
                .HasColumnType("int(11)")
                .HasColumnName("completed");
            entity.Property(e => e.Expires)
                .HasColumnType("bigint(20)")
                .HasColumnName("expires");
            entity.Property(e => e.Forfeited)
                .HasColumnType("int(11)")
                .HasColumnName("forfeited");
            entity.Property(e => e.Info)
                .HasColumnType("tinyint(3)")
                .HasColumnName("info");
            entity.Property(e => e.Quest)
                .HasColumnType("int(11)")
                .HasColumnName("quest");
            entity.Property(e => e.Status)
                .HasColumnType("int(11)")
                .HasColumnName("status");
            entity.Property(e => e.Time)
                .HasColumnType("int(11)")
                .HasColumnName("time");
        });

        modelBuilder.Entity<Quickslotkeymapped>(entity =>
        {
            entity.HasKey(e => e.Accountid).HasName("PRIMARY");

            entity.ToTable("quickslotkeymapped");

            entity.Property(e => e.Accountid)
                .HasColumnType("int(11)")
                .HasColumnName("accountid");
            entity.Property(e => e.Keymap)
                .HasColumnType("bigint(20)")
                .HasColumnName("keymap");

            entity.HasOne(d => d.Account).WithOne(p => p.Quickslotkeymapped)
                .HasForeignKey<Quickslotkeymapped>(d => d.Accountid)
                .HasConstraintName("quickslotkeymapped_accountid_fk");
        });

        modelBuilder.Entity<Reactordrop>(entity =>
        {
            entity.HasKey(e => e.Reactordropid).HasName("PRIMARY");

            entity.ToTable("reactordrops");

            entity.HasIndex(e => e.Reactorid, "reactorid");

            entity.Property(e => e.Reactordropid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("reactordropid");
            entity.Property(e => e.Chance)
                .HasColumnType("int(11)")
                .HasColumnName("chance");
            entity.Property(e => e.Itemid)
                .HasColumnType("int(11)")
                .HasColumnName("itemid");
            entity.Property(e => e.Questid)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("int(5)")
                .HasColumnName("questid");
            entity.Property(e => e.Reactorid)
                .HasColumnType("int(11)")
                .HasColumnName("reactorid");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("reports");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Chatlog)
                .HasColumnType("text")
                .HasColumnName("chatlog");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Reason)
                .HasColumnType("tinyint(4)")
                .HasColumnName("reason");
            entity.Property(e => e.Reporterid)
                .HasColumnType("int(11)")
                .HasColumnName("reporterid");
            entity.Property(e => e.Reporttime)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("reporttime");
            entity.Property(e => e.Victimid)
                .HasColumnType("int(11)")
                .HasColumnName("victimid");
        });

        modelBuilder.Entity<Response>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("responses");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Chat)
                .HasColumnType("text")
                .HasColumnName("chat");
            entity.Property(e => e.Response1)
                .HasColumnType("text")
                .HasColumnName("response");
        });

        modelBuilder.Entity<Ring_Entity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("rings");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.ItemId)
                .HasColumnType("int(11)")
                .HasColumnName("itemid");
            entity.Property(e => e.PartnerChrId)
                .HasColumnType("int(11)")
                .HasColumnName("partnerChrId");
            entity.Property(e => e.PartnerRingId)
                .HasColumnType("int(11)")
                .HasColumnName("partnerRingId");
            entity.Property(e => e.PartnerName)
                .HasMaxLength(255)
                .HasColumnName("partnername");
        });

        modelBuilder.Entity<SavedLocationEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("savedlocations");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Characterid)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
            entity.Property(e => e.Locationtype)
                .HasColumnType("enum('FREE_MARKET','WORLDTOUR','FLORINA','INTRO','SUNDAY_MARKET','MIRROR','EVENT','BOSSPQ','HAPPYVILLE','DEVELOPER','MONSTER_CARNIVAL')")
                .HasColumnName("locationtype");
            entity.Property(e => e.Map)
                .HasColumnType("int(11)")
                .HasColumnName("map");
            entity.Property(e => e.Portal)
                .HasColumnType("int(11)")
                .HasColumnName("portal");
        });

        modelBuilder.Entity<ServerQueue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("server_queue");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Accountid)
                .HasColumnType("int(11)")
                .HasColumnName("accountid");
            entity.Property(e => e.Characterid)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
            entity.Property(e => e.CreateTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("createTime");
            entity.Property(e => e.Message)
                .HasMaxLength(128)
                .HasColumnName("message");
            entity.Property(e => e.Type)
                .HasColumnType("tinyint(2)")
                .HasColumnName("type");
            entity.Property(e => e.Value)
                .HasColumnType("int(10)")
                .HasColumnName("value");
        });

        modelBuilder.Entity<DB_Shop>(entity =>
        {
            entity.HasKey(e => e.ShopId).HasName("PRIMARY");

            entity.ToTable("shops");

            entity.Property(e => e.ShopId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("shopid");
            entity.Property(e => e.NpcId)
                .HasColumnType("int(11)")
                .HasColumnName("npcid");
        });

        modelBuilder.Entity<Shopitem>(entity =>
        {
            entity.HasKey(e => e.Shopitemid).HasName("PRIMARY");

            entity.ToTable("shopitems");

            entity.Property(e => e.Shopitemid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("shopitemid");
            entity.Property(e => e.ItemId)
                .HasColumnType("int(11)")
                .HasColumnName("itemid");
            entity.Property(e => e.Pitch)
                .HasColumnType("int(11)")
                .HasColumnName("pitch");
            entity.Property(e => e.Position)
                .HasComment("sort is an arbitrary field designed to give leeway when modifying shops. The lowest number is 104 and it increments by 4 for each item to allow decent space for swapping/inserting/removing items.")
                .HasColumnType("int(11)")
                .HasColumnName("position");
            entity.Property(e => e.Price)
                .HasColumnType("int(11)")
                .HasColumnName("price");
            entity.Property(e => e.Shopid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("shopid");
        });

        modelBuilder.Entity<SkillEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("skills");

            entity.HasIndex(e => new { e.Skillid, e.Characterid }, "skillpair").IsUnique();

            entity.HasIndex(e => e.Characterid, "skills_chrid_fk");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Characterid)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
            entity.Property(e => e.Expiration)
                .HasDefaultValueSql("'-1'")
                .HasColumnType("bigint(20)")
                .HasColumnName("expiration");
            entity.Property(e => e.Masterlevel)
                .HasColumnType("int(11)")
                .HasColumnName("masterlevel");
            entity.Property(e => e.Skillid)
                .HasColumnType("int(11)")
                .HasColumnName("skillid");
            entity.Property(e => e.Skilllevel)
                .HasColumnType("int(11)")
                .HasColumnName("skilllevel");
        });

        modelBuilder.Entity<Skillmacro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("skillmacros");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Characterid)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
            entity.Property(e => e.Name)
                .HasMaxLength(13)
                .HasColumnName("name");
            entity.Property(e => e.Position).HasColumnName("position").HasColumnType("tinyint").HasDefaultValueSql("'0'");
            entity.Property(e => e.Shout).HasColumnName("shout").HasColumnType("tinyint").HasDefaultValueSql("'0'");
            entity.Property(e => e.Skill1)
                .HasColumnType("int(11)")
                .HasColumnName("skill1");
            entity.Property(e => e.Skill2)
                .HasColumnType("int(11)")
                .HasColumnName("skill2");
            entity.Property(e => e.Skill3)
                .HasColumnType("int(11)")
                .HasColumnName("skill3");
        });

        modelBuilder.Entity<Specialcashitem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("specialcashitems");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Info)
                .HasColumnType("int(1)")
                .HasColumnName("info");
            entity.Property(e => e.Modifier)
                .HasComment("1024 is add/remove")
                .HasColumnType("int(11)")
                .HasColumnName("modifier");
            entity.Property(e => e.Sn)
                .HasColumnType("int(11)")
                .HasColumnName("sn");
        });

        modelBuilder.Entity<DB_Storage>(entity =>
        {
            entity.HasKey(e => e.Storageid).HasName("PRIMARY");

            entity.ToTable("storages");

            entity.Property(e => e.Storageid)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("storageid");
            entity.Property(e => e.Accountid)
                .HasColumnType("int(11)")
                .HasColumnName("accountid");
            entity.Property(e => e.Meso)
                .HasColumnType("int(11)")
                .HasColumnName("meso");
            entity.Property(e => e.Slots)
                .HasColumnType("int(11)")
                .HasColumnName("slots");
            entity.Property(e => e.World)
                .HasColumnType("int(2)")
                .HasColumnName("world");
        });

        modelBuilder.Entity<Trocklocation>(entity =>
        {
            entity.HasKey(e => e.Trockid).HasName("PRIMARY");

            entity.ToTable("trocklocations");

            entity.Property(e => e.Trockid)
                .HasColumnType("int(11)")
                .HasColumnName("trockid");
            entity.Property(e => e.Characterid)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
            entity.Property(e => e.Mapid)
                .HasColumnType("int(11)")
                .HasColumnName("mapid");
            entity.Property(e => e.Vip)
                .HasColumnType("int(2)")
                .HasColumnName("vip");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("wishlists");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CharId)
                .HasColumnType("int(11)")
                .HasColumnName("charid");
            entity.Property(e => e.Sn)
                .HasColumnType("int(11)")
                .HasColumnName("sn");
        });

        modelBuilder.Entity<Worldtransfer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("worldtransfers");

            entity.HasIndex(e => e.Characterid, "characterid");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Characterid)
                .HasColumnType("int(11)")
                .HasColumnName("characterid");
            entity.Property(e => e.CompletionTime)
                .HasColumnType("timestamp")
                .HasColumnName("completionTime");
            entity.Property(e => e.From)
                .HasColumnType("tinyint(3)")
                .HasColumnName("from");
            entity.Property(e => e.RequestTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("requestTime");
            entity.Property(e => e.To)
                .HasColumnType("tinyint(3)")
                .HasColumnName("to");
        });

        modelBuilder.Entity<ExpLogRecord>(entity =>
        {
            entity.ToTable("characterexplogs");
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .HasColumnName("id");
            entity.Property(e => e.WorldExpRate)
                .HasColumnType("int")
                .HasColumnName("world_exp_rate");
            entity.Property(e => e.ExpCoupon)
                .HasColumnType("int")
                .HasColumnName("exp_coupon");
            entity.Property(e => e.GainedExp)
                .HasColumnType("bigint")
                .HasColumnName("gained_exp");
            entity.Property(e => e.CurrentExp)
                .HasColumnType("int")
                .HasColumnName("current_exp");
            entity.Property(e => e.ExpGainTime)
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("exp_gain_time");
            entity.Property(e => e.CharId)
                .HasColumnType("int")
                .HasColumnName("charid");
        });

        modelBuilder.Entity<GachaponPool>(entity =>
        {
            entity.ToTable("gachapon_pool");
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.Property(e => e.Name).HasColumnType("varchar(50)").IsRequired().HasDefaultValueSql("''");
        });

        modelBuilder.Entity<GachaponPoolItem>(entity =>
        {
            entity.ToTable("gachapon_pool_item");
            entity.HasKey(e => e.Id).HasName("PRIMARY");
        });


        modelBuilder.Entity<GachaponPoolLevelChance>(entity =>
        {
            entity.ToTable("gachapon_pool_level_chance");
            entity.HasKey(e => e.Id).HasName("PRIMARY");
        });

        BuildQuest(modelBuilder);
        OnModelCreatingPartial(modelBuilder);
    }

    private void BuildQuest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuestEntity>(entity =>
        {
            entity.ToTable("quest");
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasColumnType("varchar(50)").IsRequired().HasDefaultValueSql("''");
            entity.Property(e => e.ParentName).HasColumnType("varchar(50)").IsRequired().HasDefaultValueSql("''");
            entity.Property(e => e.Type).HasColumnType("varchar(50)");
        });

        modelBuilder.Entity<QuestRequirementEntity>(entity =>
        {
            entity.ToTable("quest_requirement");
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.Property(e => e.RequirementType).HasColumnType("varchar(20)").IsRequired().HasDefaultValueSql("''");
            entity.HasIndex(e => e.QuestId, "idx_qid");
        });

        modelBuilder.Entity<QuestRewardEntity>(entity =>
        {
            entity.ToTable("quest_reward");
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.Property(e => e.RewardType).HasColumnType("varchar(20)").IsRequired().HasDefaultValueSql("''");
            entity.HasIndex(e => e.QuestId, "idx_qid");
        });

    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
