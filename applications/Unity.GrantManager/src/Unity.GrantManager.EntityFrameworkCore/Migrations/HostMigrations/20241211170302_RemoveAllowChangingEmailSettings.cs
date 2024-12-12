using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class RemoveAllowChangingEmailSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM public.\"PermissionGrants\" WHERE \"Name\" IN ('SettingManagement.Emailing','SettingManagement.Emailing.Test','SettingManagement.TimeZone')");
            migrationBuilder.Sql($"DELETE FROM public.\"FeatureValues\" WHERE \"Name\" = 'SettingManagement.AllowChangingEmailSettings'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // NOTE: Manually add SettingManagement.AllowChangingEmailSettings feature and SettingManagement.Emailing permissions for individual tenants
        }
    }
}
