using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolithic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftTemplateManagementDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "BreakEnd",
                table: "ShiftTemplates",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "BreakStart",
                table: "ShiftTemplates",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                table: "ShiftTemplates",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ShiftTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExcludePublicHolidays",
                table: "ShiftTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "ShiftTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByDisplayName",
                table: "ShiftTemplates",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "ShiftTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "WorkingDaysMask",
                table: "ShiftTemplates",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)62);

            migrationBuilder.Sql("""
                UPDATE "ShiftTemplates"
                SET "ExcludePublicHolidays" = TRUE,
                    "WorkingDaysMask" = 62
                WHERE "WorkingDaysMask" = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BreakEnd",
                table: "ShiftTemplates");

            migrationBuilder.DropColumn(
                name: "BreakStart",
                table: "ShiftTemplates");

            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                table: "ShiftTemplates");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ShiftTemplates");

            migrationBuilder.DropColumn(
                name: "ExcludePublicHolidays",
                table: "ShiftTemplates");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "ShiftTemplates");

            migrationBuilder.DropColumn(
                name: "ModifiedByDisplayName",
                table: "ShiftTemplates");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "ShiftTemplates");

            migrationBuilder.DropColumn(
                name: "WorkingDaysMask",
                table: "ShiftTemplates");
        }
    }
}
