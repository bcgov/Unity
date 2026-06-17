using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AB33225_EmailSentDate_Datafix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Notifications"".""EmailLogs""
                SET ""SentDateTime"" = COALESCE(""LastModificationTime"", ""CreationTime"")
                WHERE ""Status"" = 'Sent'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Notifications"".""EmailLogs""
                SET ""SentDateTime"" = NULL
                WHERE ""Status"" = 'Sent'
            ");
        }
    }
}
