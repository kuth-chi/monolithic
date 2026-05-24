using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

#nullable disable

namespace Monolithic.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260524132000_AddCompensationLeaveByEmployee")]
    public partial class AddCompensationLeaveByEmployee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompensationLeaveByEmployeeJson",
                table: "BusinessSettings",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompensationLeaveByEmployeeJson",
                table: "BusinessSettings");
        }
    }
}
