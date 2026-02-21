using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolithic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeatureFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Channel = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateSl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Recipient = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemplateDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AvailableVariables = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ActiveVersionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ThemeProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    ColorPrimary = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ColorSecondary = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ColorAccent = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ColorSuccess = table.Column<string>(type: "TEXT", nullable: false),
                    ColorWarning = table.Column<string>(type: "TEXT", nullable: false),
                    ColorDanger = table.Column<string>(type: "TEXT", nullable: false),
                    ColorInfo = table.Column<string>(type: "TEXT", nullable: false),
                    ColorBackground = table.Column<string>(type: "TEXT", nullable: false),
                    ColorSurface = table.Column<string>(type: "TEXT", nullable: false),
                    ColorBorder = table.Column<string>(type: "TEXT", nullable: false),
                    ColorText = table.Column<string>(type: "TEXT", nullable: false),
                    ColorTextMuted = table.Column<string>(type: "TEXT", nullable: false),
                    FontFamily = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FontFamilyMono = table.Column<string>(type: "TEXT", nullable: false),
                    FontSizeBase = table.Column<string>(type: "TEXT", nullable: false),
                    FontScaleRatio = table.Column<decimal>(type: "TEXT", nullable: false),
                    SpacingUnit = table.Column<int>(type: "INTEGER", nullable: false),
                    BorderRadiusSm = table.Column<string>(type: "TEXT", nullable: false),
                    BorderRadiusMd = table.Column<string>(type: "TEXT", nullable: false),
                    BorderRadiusLg = table.Column<string>(type: "TEXT", nullable: false),
                    BorderRadiusFull = table.Column<string>(type: "TEXT", nullable: false),
                    ShadowSm = table.Column<string>(type: "TEXT", nullable: false),
                    ShadowMd = table.Column<string>(type: "TEXT", nullable: false),
                    ShadowLg = table.Column<string>(type: "TEXT", nullable: false),
                    SidebarWidth = table.Column<string>(type: "TEXT", nullable: false),
                    TopbarHeight = table.Column<string>(type: "TEXT", nullable: false),
                    ContentMaxWidth = table.Column<string>(type: "TEXT", nullable: false),
                    SidebarPosition = table.Column<string>(type: "TEXT", nullable: false),
                    ExtensionTokensJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThemeProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PreferredLocale = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PreferredTimezone = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    PreferredThemeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ColorScheme = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DashboardLayoutJson = table.Column<string>(type: "TEXT", nullable: true),
                    EmailNotificationsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SmsNotificationsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PushNotificationsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemplateVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TemplateDefinitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    PlainTextFallback = table.Column<string>(type: "TEXT", nullable: true),
                    VersionLabel = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ChangeNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateVersions_TemplateDefinitions_TemplateDefinitionId",
                        column: x => x.TemplateDefinitionId,
                        principalTable: "TemplateDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_Key_Scope_BusinessId_UserId",
                table: "FeatureFlags",
                columns: new[] { "Key", "Scope", "BusinessId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_BusinessId_CreatedAtUtc",
                table: "NotificationLogs",
                columns: new[] { "BusinessId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_UserId_Status",
                table: "NotificationLogs",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateDefinitions_Slug_Scope_BusinessId_UserId",
                table: "TemplateDefinitions",
                columns: new[] { "Slug", "Scope", "BusinessId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateVersions_TemplateDefinitionId",
                table: "TemplateVersions",
                column: "TemplateDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ThemeProfiles_BusinessId_Name",
                table: "ThemeProfiles",
                columns: new[] { "BusinessId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId_BusinessId",
                table: "UserPreferences",
                columns: new[] { "UserId", "BusinessId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureFlags");

            migrationBuilder.DropTable(
                name: "NotificationLogs");

            migrationBuilder.DropTable(
                name: "TemplateVersions");

            migrationBuilder.DropTable(
                name: "ThemeProfiles");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "TemplateDefinitions");
        }
    }
}
