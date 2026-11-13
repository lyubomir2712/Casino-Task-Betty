using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Games.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Create_Games_DB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BetCents = table.Column<long>(type: "bigint", nullable: false),
                    WinCents = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BalanceAfterCents = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rounds", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rounds_PlayerId",
                table: "rounds",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_rounds_Status_CreatedAt",
                table: "rounds",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rounds");
        }
    }
}
