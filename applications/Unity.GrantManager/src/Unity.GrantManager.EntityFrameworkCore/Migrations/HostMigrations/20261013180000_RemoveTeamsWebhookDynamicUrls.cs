using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Unity.GrantManager.EntityFrameworkCore;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations;

[DbContext(typeof(GrantManagerDbContext))]
[Migration("20261013180000_RemoveTeamsWebhookDynamicUrls")]
public partial class RemoveTeamsWebhookDynamicUrls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            -- DynamicUrlDataSeeder no longer seeds these Teams/webhook endpoint rows; nothing posts to them.
            -- Hard delete (soft-deleted rows included) because a set row holds a secret webhook URL.
            DELETE FROM public."DynamicUrls"
            WHERE "KeyName" IN ('DIRECT_MESSAGE_0', 'DIRECT_MESSAGE_1', 'DIRECT_MESSAGE_2', 'WEBHOOK_0', 'WEBHOOK_1', 'WEBHOOK_2');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Not reversible: the deleted webhook URLs are not recoverable, and the rows are no longer used.
    }
}
