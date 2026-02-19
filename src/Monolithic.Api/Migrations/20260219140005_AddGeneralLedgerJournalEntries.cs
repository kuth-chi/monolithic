using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolithic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneralLedgerJournalEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntryNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    FiscalPeriod = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    SourceDocumentReference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SourceDocumentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReversalOfEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReversedByEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    TotalDebits = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalCredits = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    PostedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PostedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ReversedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReversedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntries_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntries_JournalEntries_ReversalOfEntryId",
                        column: x => x.ReversalOfEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntryAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserDisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntryAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntryAuditLogs_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntryLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    DebitAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DebitAmountBase = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CreditAmountBase = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CostCenter = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ProjectCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    LineDescription = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntryLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_ChartOfAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "ChartOfAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_BusinessId_EntryNumber",
                table: "JournalEntries",
                columns: new[] { "BusinessId", "EntryNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_BusinessId_FiscalPeriod",
                table: "JournalEntries",
                columns: new[] { "BusinessId", "FiscalPeriod" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_BusinessId_TransactionDate",
                table: "JournalEntries",
                columns: new[] { "BusinessId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_ReversalOfEntryId",
                table: "JournalEntries",
                column: "ReversalOfEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_Status",
                table: "JournalEntries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryAuditLogs_JournalEntryId",
                table: "JournalEntryAuditLogs",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryAuditLogs_OccurredAtUtc",
                table: "JournalEntryAuditLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_AccountId",
                table: "JournalEntryLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_JournalEntryId_LineNumber",
                table: "JournalEntryLines",
                columns: new[] { "JournalEntryId", "LineNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JournalEntryAuditLogs");

            migrationBuilder.DropTable(
                name: "JournalEntryLines");

            migrationBuilder.DropTable(
                name: "JournalEntries");
        }
    }
}
