using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Unity.GrantManager.EntityFrameworkCore;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    [DbContext(typeof(GrantTenantDbContext))]
    [Migration("20260909120000_AddDateNotificationStatusFilters")]
    public partial class AddDateNotificationStatusFilters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationStatusIds",
                schema: "Notifications",
                table: "ScheduledNotifications",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationStatusIds",
                schema: "Notifications",
                table: "ScheduledNotifications");
        }
    }
}