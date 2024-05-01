using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
#pragma warning disable IDE1006 // Naming Styles
    public partial class addTenantToTags : Migration
#pragma warning restore IDE1006 // Naming Styles
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
#pragma warning disable S4581 // "new Guid()" should not be used
            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationId",
                table: "ApplicationTags",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
#pragma warning restore S4581 // "new Guid()" should not be used

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ApplicationTags",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApplicationTags");

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicationId",
                table: "ApplicationTags",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
