using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class DropAppTagsFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
            name: "FK_ApplicationTags_Tags_TagId",
            table: "ApplicationTags");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
            name: "FK_ApplicationTags_Tags_TagId",
            table: "ApplicationTags",
            column: "TagId",
            principalTable: "Tags",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
        }
    }
}
