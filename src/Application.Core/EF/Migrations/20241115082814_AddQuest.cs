using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Application.Core.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddQuest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "questactions");

            migrationBuilder.DropTable(
                name: "questrequirements");

            migrationBuilder.CreateTable(
                name: "quest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", nullable: false, defaultValueSql: "''"),
                    ParentName = table.Column<string>(type: "varchar(50)", nullable: false, defaultValueSql: "''"),
                    TimeLimit = table.Column<int>(type: "int", nullable: false),
                    TimeLimit2 = table.Column<int>(type: "int", nullable: false),
                    AutoStart = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AutoPreComplete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AutoComplete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MedalId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "varchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "quest_requirement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Step = table.Column<int>(type: "int", nullable: false),
                    QuestId = table.Column<int>(type: "int", nullable: false),
                    RequirementType = table.Column<string>(type: "varchar(20)", nullable: false, defaultValueSql: "''"),
                    Value = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "quest_reward",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Step = table.Column<int>(type: "int", nullable: false),
                    QuestId = table.Column<int>(type: "int", nullable: false),
                    RewardType = table.Column<string>(type: "varchar(20)", nullable: false, defaultValueSql: "''"),
                    Value = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_qid",
                table: "quest_requirement",
                column: "QuestId");

            migrationBuilder.CreateIndex(
                name: "idx_qid1",
                table: "quest_reward",
                column: "QuestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quest");

            migrationBuilder.DropTable(
                name: "quest_requirement");

            migrationBuilder.DropTable(
                name: "quest_reward");

            migrationBuilder.CreateTable(
                name: "questactions",
                columns: table => new
                {
                    questactionid = table.Column<int>(type: "int(10) unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    data = table.Column<byte[]>(type: "blob", nullable: false),
                    questid = table.Column<int>(type: "int(11)", nullable: false),
                    status = table.Column<int>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.questactionid);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "questrequirements",
                columns: table => new
                {
                    questrequirementid = table.Column<int>(type: "int(10) unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    data = table.Column<byte[]>(type: "blob", nullable: false),
                    questid = table.Column<int>(type: "int(11)", nullable: false),
                    status = table.Column<int>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.questrequirementid);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }
    }
}
