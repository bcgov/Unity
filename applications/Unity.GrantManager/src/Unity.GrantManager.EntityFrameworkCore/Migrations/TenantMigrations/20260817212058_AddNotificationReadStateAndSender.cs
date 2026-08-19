using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations;

public partial class AddNotificationMessagingTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "Notifications");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ""Notifications"".""NotificationLogs"" (
                ""Id"" uuid NOT NULL,
                ""TenantId"" uuid,
                ""UserId"" uuid,
                ""SenderUserId"" uuid,
                ""SenderDisplayName"" character varying(256),
                ""NotificationType"" character varying(64) NOT NULL,
                ""Channel"" character varying(32) NOT NULL,
                ""Severity"" character varying(32) NOT NULL,
                ""Title"" character varying(256) NOT NULL,
                ""Message"" text NOT NULL,
                ""Source"" character varying(200) NOT NULL,
                ""SourceReference"" character varying(256),
                ""PayloadJson"" jsonb,
                ""CorrelationId"" character varying(128),
                ""IsDeliveredRealtime"" boolean NOT NULL,
                ""DeliveryTarget"" character varying(256),
                ""ExceptionType"" character varying(256),
                ""ExceptionMessage"" text,
                ""StackExcerpt"" text,
                ""CommitSha"" character varying(64),
                ""Environment"" character varying(64),
                ""ExtraProperties"" text NOT NULL,
                ""ConcurrencyStamp"" character varying(40) NOT NULL,
                ""CreationTime"" timestamp without time zone NOT NULL,
                ""CreatorId"" uuid,
                ""LastModificationTime"" timestamp without time zone,
                ""LastModifierId"" uuid,
                CONSTRAINT ""PK_NotificationLogs"" PRIMARY KEY (""Id"")
            );

            ALTER TABLE ""Notifications"".""NotificationLogs""
                ADD COLUMN IF NOT EXISTS ""SenderUserId"" uuid;
            ALTER TABLE ""Notifications"".""NotificationLogs""
                ADD COLUMN IF NOT EXISTS ""SenderDisplayName"" character varying(256);

            CREATE INDEX IF NOT EXISTS ""IX_NotificationLogs_CorrelationId""
                ON ""Notifications"".""NotificationLogs"" (""CorrelationId"");
            CREATE INDEX IF NOT EXISTS ""IX_NotificationLogs_NotificationType_CreationTime""
                ON ""Notifications"".""NotificationLogs"" (""NotificationType"", ""CreationTime"");
            CREATE INDEX IF NOT EXISTS ""IX_NotificationLogs_TenantId_CreationTime""
                ON ""Notifications"".""NotificationLogs"" (""TenantId"", ""CreationTime"");

            CREATE TABLE IF NOT EXISTS ""Notifications"".""NotificationReadStates"" (
                ""Id"" uuid NOT NULL,
                ""TenantId"" uuid,
                ""UserId"" uuid NOT NULL,
                ""LastReadAt"" timestamp without time zone NOT NULL,
                ""ExtraProperties"" text NOT NULL,
                ""ConcurrencyStamp"" character varying(40) NOT NULL,
                ""CreationTime"" timestamp without time zone NOT NULL,
                ""CreatorId"" uuid,
                ""LastModificationTime"" timestamp without time zone,
                ""LastModifierId"" uuid,
                CONSTRAINT ""PK_NotificationReadStates"" PRIMARY KEY (""Id"")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_NotificationReadStates_TenantId_UserId""
                ON ""Notifications"".""NotificationReadStates"" (""TenantId"", ""UserId"");
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "NotificationReadStates",
            schema: "Notifications");

        migrationBuilder.DropTable(
            name: "NotificationLogs",
            schema: "Notifications");
    }
}
