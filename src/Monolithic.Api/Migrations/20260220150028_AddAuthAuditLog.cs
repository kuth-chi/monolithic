using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolithic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Event = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PreviousBusinessId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthAuditLogs_Email",
                table: "AuthAuditLogs",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_AuthAuditLogs_Event_OccurredAtUtc",
                table: "AuthAuditLogs",
                columns: new[] { "Event", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthAuditLogs_OccurredAtUtc",
                table: "AuthAuditLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuthAuditLogs_UserId",
                table: "AuthAuditLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthAuditLogs");
        }
    }
}
