using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

using Unity.GrantManager.EntityFrameworkCore;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations;

[DbContext(typeof(GrantManagerDbContext))]
[Migration("20261013120000_AddQuartzMisfireOriginalFireTime")]
public partial class AddQuartzMisfireOriginalFireTime : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "misfire_orig_fire_time",
            table: "qrtz_triggers",
            type: "bigint",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "misfire_orig_fire_time",
            table: "qrtz_triggers");
    }
}