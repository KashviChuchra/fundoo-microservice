using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fundoo.User.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailOtpChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailOtpChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailOtpChallenges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailOtpChallenges_ExpiresAtUtc",
                table: "EmailOtpChallenges",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmailOtpChallenges_UserId",
                table: "EmailOtpChallenges",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailOtpChallenges");
        }
    }
}
