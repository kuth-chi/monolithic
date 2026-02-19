using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolithic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiBusinessBranchMVP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessBranches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    IsHeadquarters = table.Column<bool>(type: "INTEGER", nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StateProvince = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    TimezoneId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ManagerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessBranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessBranches_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessHolidays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IsRecurring = table.Column<bool>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessHolidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessHolidays_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessLicenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Plan = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    MaxBusinesses = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxBranchesPerBusiness = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxEmployees = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowAdvancedReporting = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowMultiCurrency = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowIntegrations = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ExpiresOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ExternalSubscriptionId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessLicenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MediaType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    PublicUrl = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    AltText = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    IsCurrent = table.Column<bool>(type: "INTEGER", nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessMedia_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrimaryColor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SecondaryColor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    AccentColor = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    TimezoneId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayCurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Locale = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    WeekStartDay = table.Column<int>(type: "INTEGER", nullable: false),
                    FiscalYearStartMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    HolidayCountryCode = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    AutoImportPublicHolidays = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultShiftStart = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    DefaultShiftEnd = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    LateGraceMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    ManagerCanViewAttendance = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmployeeCanViewOwnAttendance = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultReportRangeDays = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessSettings_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttendancePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BranchId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Scope = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Department = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ShiftStart = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    ShiftEnd = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    BreakMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    LateGraceMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredHoursPerDay = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    WorkingDaysMask = table.Column<byte>(type: "INTEGER", nullable: false),
                    EmployeeCanViewOwn = table.Column<bool>(type: "INTEGER", nullable: false),
                    ManagerCanView = table.Column<bool>(type: "INTEGER", nullable: false),
                    HrCanView = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcludedHolidayIds = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendancePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendancePolicies_BusinessBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "BusinessBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendancePolicies_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BranchEmployees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BranchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    AssignedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ReleasedOn = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchEmployees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchEmployees_BusinessBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "BusinessBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessOwnerships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LicenseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsPrimaryOwner = table.Column<bool>(type: "INTEGER", nullable: false),
                    GrantedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessOwnerships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessOwnerships_BusinessLicenses_LicenseId",
                        column: x => x.LicenseId,
                        principalTable: "BusinessLicenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessOwnerships_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePolicies_BranchId",
                table: "AttendancePolicies",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendancePolicies_BusinessId_BranchId_Scope",
                table: "AttendancePolicies",
                columns: new[] { "BusinessId", "BranchId", "Scope" });

            migrationBuilder.CreateIndex(
                name: "IX_BranchEmployees_BranchId_EmployeeId",
                table: "BranchEmployees",
                columns: new[] { "BranchId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessBranches_BusinessId_Code",
                table: "BusinessBranches",
                columns: new[] { "BusinessId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessBranches_BusinessId_IsHeadquarters",
                table: "BusinessBranches",
                columns: new[] { "BusinessId", "IsHeadquarters" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessHolidays_BusinessId_Date",
                table: "BusinessHolidays",
                columns: new[] { "BusinessId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessHolidays_BusinessId_ExternalId",
                table: "BusinessHolidays",
                columns: new[] { "BusinessId", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessLicenses_OwnerId",
                table: "BusinessLicenses",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMedia_BusinessId_MediaType_IsCurrent",
                table: "BusinessMedia",
                columns: new[] { "BusinessId", "MediaType", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOwnerships_BusinessId",
                table: "BusinessOwnerships",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOwnerships_LicenseId",
                table: "BusinessOwnerships",
                column: "LicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOwnerships_OwnerId_BusinessId",
                table: "BusinessOwnerships",
                columns: new[] { "OwnerId", "BusinessId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOwnerships_OwnerId_RevokedAtUtc",
                table: "BusinessOwnerships",
                columns: new[] { "OwnerId", "RevokedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSettings_BusinessId",
                table: "BusinessSettings",
                column: "BusinessId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendancePolicies");

            migrationBuilder.DropTable(
                name: "BranchEmployees");

            migrationBuilder.DropTable(
                name: "BusinessHolidays");

            migrationBuilder.DropTable(
                name: "BusinessMedia");

            migrationBuilder.DropTable(
                name: "BusinessOwnerships");

            migrationBuilder.DropTable(
                name: "BusinessSettings");

            migrationBuilder.DropTable(
                name: "BusinessBranches");

            migrationBuilder.DropTable(
                name: "BusinessLicenses");
        }
    }
}
