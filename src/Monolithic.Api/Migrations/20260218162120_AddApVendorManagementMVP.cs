using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolithic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddApVendorManagementMVP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApCreditNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalVendorBillId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreditNoteNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    VendorReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CreditAmountBase = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AmountApplied = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AmountRemaining = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApCreditNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApCreditNotes_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApCreditNotes_VendorBills_OriginalVendorBillId",
                        column: x => x.OriginalVendorBillId,
                        principalTable: "VendorBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ApCreditNotes_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApPaymentSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentMode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BankAccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalAmountBase = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PostedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApPaymentSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApPaymentSessions_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ApPaymentSessions_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApPaymentSessions_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendorClasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ColorHex = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorClasses_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorCreditTerms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NetDays = table.Column<int>(type: "INTEGER", nullable: false),
                    EarlyPayDiscountPercent = table.Column<decimal>(type: "TEXT", precision: 8, scale: 4, nullable: false),
                    EarlyPayDiscountDays = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCod = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorCreditTerms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorCreditTerms_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApCreditNoteApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreditNoteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendorBillId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ApplicationDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApCreditNoteApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApCreditNoteApplications_ApCreditNotes_CreditNoteId",
                        column: x => x.CreditNoteId,
                        principalTable: "ApCreditNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApCreditNoteApplications_VendorBills_VendorBillId",
                        column: x => x.VendorBillId,
                        principalTable: "VendorBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApPaymentSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendorBillId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ScheduledAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    BankAccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ExecutedSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ExecutedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApPaymentSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApPaymentSchedules_ApPaymentSessions_ExecutedSessionId",
                        column: x => x.ExecutedSessionId,
                        principalTable: "ApPaymentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ApPaymentSchedules_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApPaymentSchedules_VendorBills_VendorBillId",
                        column: x => x.VendorBillId,
                        principalTable: "VendorBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApPaymentSchedules_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApPaymentSessionLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendorBillId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    BillAmountDueBefore = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    BillAmountDueAfter = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    IsPartialPayment = table.Column<bool>(type: "INTEGER", nullable: false),
                    VendorBillPaymentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApPaymentSessionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApPaymentSessionLines_ApPaymentSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ApPaymentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApPaymentSessionLines_VendorBillPayments_VendorBillPaymentId",
                        column: x => x.VendorBillPaymentId,
                        principalTable: "VendorBillPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ApPaymentSessionLines_VendorBills_VendorBillId",
                        column: x => x.VendorBillId,
                        principalTable: "VendorBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendorProfiles",
                columns: table => new
                {
                    VendorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DefaultVatPercent = table.Column<decimal>(type: "TEXT", precision: 8, scale: 4, nullable: false),
                    VatRegistrationNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsVatRegistered = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreditTermId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreditTermDaysOverride = table.Column<int>(type: "INTEGER", nullable: true),
                    CreditLimitBase = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    PreferredPaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PreferredBankAccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MinimumPaymentAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    VendorClassId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PerformanceRating = table.Column<decimal>(type: "TEXT", precision: 3, scale: 2, nullable: false),
                    RelationshipNotes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    IsOnHold = table.Column<bool>(type: "INTEGER", nullable: false),
                    HoldReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsBlacklisted = table.Column<bool>(type: "INTEGER", nullable: false),
                    BlacklistReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorProfiles", x => x.VendorId);
                    table.ForeignKey(
                        name: "FK_VendorProfiles_BankAccounts_PreferredBankAccountId",
                        column: x => x.PreferredBankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VendorProfiles_VendorClasses_VendorClassId",
                        column: x => x.VendorClassId,
                        principalTable: "VendorClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VendorProfiles_VendorCreditTerms_CreditTermId",
                        column: x => x.CreditTermId,
                        principalTable: "VendorCreditTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VendorProfiles_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApCreditNoteApplications_CreditNoteId_VendorBillId",
                table: "ApCreditNoteApplications",
                columns: new[] { "CreditNoteId", "VendorBillId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApCreditNoteApplications_VendorBillId",
                table: "ApCreditNoteApplications",
                column: "VendorBillId");

            migrationBuilder.CreateIndex(
                name: "IX_ApCreditNotes_BusinessId_CreditNoteNumber",
                table: "ApCreditNotes",
                columns: new[] { "BusinessId", "CreditNoteNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApCreditNotes_BusinessId_VendorId_Status",
                table: "ApCreditNotes",
                columns: new[] { "BusinessId", "VendorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApCreditNotes_OriginalVendorBillId",
                table: "ApCreditNotes",
                column: "OriginalVendorBillId");

            migrationBuilder.CreateIndex(
                name: "IX_ApCreditNotes_VendorId",
                table: "ApCreditNotes",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ApPaymentSchedules_BusinessId_VendorId_Status_ScheduledDate",
                table: "ApPaymentSchedules",
                columns: new[] { "BusinessId", "VendorId", "Status", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ApPaymentSchedules_ExecutedSessionId",
                table: "ApPaymentSchedules",
                column: "ExecutedSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ApPaymentSchedules_VendorBillId",
                table: "ApPaymentSchedules",
                column: "VendorBillId");

            migrationBuilder.CreateIndex(
                name: "IX_ApPaymentSchedules_VendorId",
                table: "ApPaymentSchedules",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ApPaymentSessionLines_SessionId_VendorBillId",
                table: "ApPaymentSessionLines",
                columns: new[] { "SessionId", "VendorBillId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApPaymentSessionLines_VendorBillId",
                table: "ApPaymentSessionLines",
                column: "VendorBillId");

            migrationBuilder.CreateIndex(
                name: "IX_ApPaymentSessionLines_VendorBillPaymentId",
                table: "ApPaymentSessionLines",
                column: "VendorBillPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ApPaymentSessions_BankAccountId",
                table: "ApPaymentSessions",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ApPaymentSessions_BusinessId_VendorId_Status",
                table: "ApPaymentSessions",
                columns: new[] { "BusinessId", "VendorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApPaymentSessions_VendorId",
                table: "ApPaymentSessions",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorClasses_BusinessId_Code",
                table: "VendorClasses",
                columns: new[] { "BusinessId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorCreditTerms_BusinessId_Name",
                table: "VendorCreditTerms",
                columns: new[] { "BusinessId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorProfiles_CreditTermId",
                table: "VendorProfiles",
                column: "CreditTermId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorProfiles_PreferredBankAccountId",
                table: "VendorProfiles",
                column: "PreferredBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorProfiles_VendorClassId",
                table: "VendorProfiles",
                column: "VendorClassId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApCreditNoteApplications");

            migrationBuilder.DropTable(
                name: "ApPaymentSchedules");

            migrationBuilder.DropTable(
                name: "ApPaymentSessionLines");

            migrationBuilder.DropTable(
                name: "VendorProfiles");

            migrationBuilder.DropTable(
                name: "ApCreditNotes");

            migrationBuilder.DropTable(
                name: "ApPaymentSessions");

            migrationBuilder.DropTable(
                name: "VendorClasses");

            migrationBuilder.DropTable(
                name: "VendorCreditTerms");
        }
    }
}
