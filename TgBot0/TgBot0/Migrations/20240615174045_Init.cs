using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TgBot0.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatStats",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CasinoBalance = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatStats", x => x.ChatId);
                });

            migrationBuilder.CreateTable(
                name: "ChatPlayerStats",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    SpinsLost = table.Column<int>(type: "integer", nullable: false),
                    SpinsWon = table.Column<int>(type: "integer", nullable: false),
                    ScoreWon = table.Column<int>(type: "integer", nullable: false),
                    TotalScore = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatPlayerStats", x => new { x.ChatId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ChatPlayerStats_ChatStats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "ChatStats",
                        principalColumn: "ChatId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatPlayerStats");

            migrationBuilder.DropTable(
                name: "ChatStats");
        }
    }
}
