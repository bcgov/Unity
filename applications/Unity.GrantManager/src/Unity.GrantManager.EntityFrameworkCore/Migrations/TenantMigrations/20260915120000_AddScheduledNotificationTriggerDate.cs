using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Unity.GrantManager.EntityFrameworkCore;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    [DbContext(typeof(GrantTenantDbContext))]
    [Migration("20260915120000_AddScheduledNotificationTriggerDate")]
    public partial class AddScheduledNotificationTriggerDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
DECLARE
    index_name text;
BEGIN
    IF to_regclass('""Notifications"".""ScheduledNotificationTracking""') IS NOT NULL THEN
        ALTER TABLE ""Notifications"".""ScheduledNotificationTracking""
            ADD COLUMN IF NOT EXISTS ""TriggerDate"" timestamp without time zone;

        FOR index_name IN
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'Notifications'
              AND tablename = 'ScheduledNotificationTracking'
              AND indexdef LIKE 'CREATE UNIQUE INDEX%'
              AND indexdef LIKE '%(""ApplicationId"", ""ScheduledNotificationId"", ""DateField"")%'
        LOOP
            EXECUTE format('DROP INDEX IF EXISTS %I.%I', 'Notifications', index_name);
        END LOOP;

        CREATE UNIQUE INDEX IF NOT EXISTS ""UX_ScheduledNotificationTracking_App_Notification_Field_Date""
            ON ""Notifications"".""ScheduledNotificationTracking""
            (""ApplicationId"", ""ScheduledNotificationId"", ""DateField"", ""TriggerDate"");
    END IF;
END $$;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF to_regclass('""Notifications"".""ScheduledNotificationTracking""') IS NOT NULL THEN
        DROP INDEX IF EXISTS ""Notifications"".""UX_ScheduledNotificationTracking_App_Notification_Field_Date"";
        ALTER TABLE ""Notifications"".""ScheduledNotificationTracking""
            DROP COLUMN IF EXISTS ""TriggerDate"";
    END IF;
END $$;");
        }
    }
}