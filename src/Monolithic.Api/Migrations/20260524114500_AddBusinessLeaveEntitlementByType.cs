using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

#nullable disable

namespace Monolithic.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260524114500_AddBusinessLeaveEntitlementByType")]
    public partial class AddBusinessLeaveEntitlementByType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LeaveEntitlementByTypeJson",
                table: "BusinessSettings",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LeaveEntitlementByTypeJson",
                table: "BusinessSettings");
        }
    }
}
