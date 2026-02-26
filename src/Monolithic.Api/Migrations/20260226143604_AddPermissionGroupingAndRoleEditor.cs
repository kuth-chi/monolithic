using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolithic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionGroupingAndRoleEditor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActionName",
                table: "Permissions",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FeatureName",
                table: "Permissions",
                type: "TEXT",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "Permissions",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Permissions",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_GroupName_FeatureName_ActionName",
                table: "Permissions",
                columns: new[] { "GroupName", "FeatureName", "ActionName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Permissions_GroupName_FeatureName_ActionName",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "ActionName",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "FeatureName",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Permissions");
        }
    }
}
