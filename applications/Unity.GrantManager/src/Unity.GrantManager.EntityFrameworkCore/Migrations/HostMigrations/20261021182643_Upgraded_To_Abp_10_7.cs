using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class Upgraded_To_Abp_10_7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BackgroundJobs_IsAbandoned_NextTryTime",
                table: "BackgroundJobs");

            // qrtz_triggers.misfire_orig_fire_time is not added here: migration
            // 20261013120000_AddQuartzMisfireOriginalFireTime already creates it,
            // but it did not update the model snapshot. This migration only
            // brings the snapshot in line for that column.

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletionTime",
                table: "BackgroundJobs",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_ApplicationName_CompletionTime_IsAbandoned_N~",
                table: "BackgroundJobs",
                columns: new[] { "ApplicationName", "CompletionTime", "IsAbandoned", "NextTryTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BackgroundJobs_ApplicationName_CompletionTime_IsAbandoned_N~",
                table: "BackgroundJobs");

            migrationBuilder.DropColumn(
                name: "CompletionTime",
                table: "BackgroundJobs");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_IsAbandoned_NextTryTime",
                table: "BackgroundJobs",
                columns: new[] { "IsAbandoned", "NextTryTime" });
        }
    }
}
