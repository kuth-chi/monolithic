using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolithic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseTamperDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsSelfScoped",
                table: "Permissions",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastTamperDetectedAtUtc",
                table: "BusinessLicenses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TamperCount",
                table: "BusinessLicenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TamperWarningMessage",
                table: "BusinessLicenses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SuspendedAtUtc",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspendedReason",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTamperDetectedAtUtc",
                table: "BusinessLicenses");

            migrationBuilder.DropColumn(
                name: "TamperCount",
                table: "BusinessLicenses");

            migrationBuilder.DropColumn(
                name: "TamperWarningMessage",
                table: "BusinessLicenses");

            migrationBuilder.DropColumn(
                name: "SuspendedAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SuspendedReason",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<bool>(
                name: "IsSelfScoped",
                table: "Permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");
        }
    }
}
