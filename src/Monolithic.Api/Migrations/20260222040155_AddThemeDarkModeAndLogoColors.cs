using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolithic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddThemeDarkModeAndLogoColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorAccentDark",
                table: "ThemeProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorBackgroundDark",
                table: "ThemeProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorBorderDark",
                table: "ThemeProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorDangerDark",
                table: "ThemeProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorInfoDark",
                table: "ThemeProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorPrimaryDark",
                table: "ThemeProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorSecondaryDark",
                table: "ThemeProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorSuccessDark",
                table: "ThemeProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorSurfaceDark",
                table: "ThemeProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorTextDark",
                table: "ThemeProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorTextMutedDark",
                table: "ThemeProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorWarningDark",
                table: "ThemeProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LogoColorsExtractedAtUtc",
                table: "ThemeProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LogoColorsOverridden",
                table: "ThemeProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LogoExtractedColorsJson",
                table: "ThemeProfiles",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorAccentDark",
                table: "ThemeProfiles");

            migrationBuilder.DropColumn(
                name: "ColorBackgroundDark",
                table: "ThemeProfiles");

            migrationBuilder.DropColumn(
                name: "ColorBorderDark",
                table: "ThemeProfiles");

            migrationBuilder.DropColumn(
                name: "ColorDangerDark",
                table: "ThemeProfiles");

            migrationBuilder.DropColumn(
                name: "ColorInfoDark",
                table: "ThemeProfiles");

            migrationBuilder.DropColumn(
                name: "ColorPrimaryDark",
                table: "ThemeProfiles");

            migrationBuilder.DropColumn(
                name: "ColorSecondaryDark",
                table: "ThemeProfiles");

            migrationBuilder.DropColumn(
                name: "ColorSuccessDark",
                table: "ThemeProfiles");

            migrationBuilder.DropColumn(
                name: "ColorSurfaceDark",
                table: "ThemeProfiles");

            migrationBuilder.DropColumn(
                name: "ColorTextDark",
                table: "ThemeProfiles");

            migrationBuilder.DropColumn(
                name: "ColorTextMutedDark",
                table: "ThemeProfiles");

            migrationBuilder.DropColumn(
                name: "ColorWarningDark",
                table: "ThemeProfiles");

            migrationBuilder.DropColumn(
                name: "LogoColorsExtractedAtUtc",
                table: "ThemeProfiles");

            migrationBuilder.DropColumn(
                name: "LogoColorsOverridden",
                table: "ThemeProfiles");

            migrationBuilder.DropColumn(
                name: "LogoExtractedColorsJson",
                table: "ThemeProfiles");
        }
    }
}
