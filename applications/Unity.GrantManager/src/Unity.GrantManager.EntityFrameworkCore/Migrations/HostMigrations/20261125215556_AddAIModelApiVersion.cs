using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class AddAIModelApiVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiVersion",
                schema: "AI",
                table: "AIModels",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            // Existing models keep calling Azure the way they did before this column existed:
            // 2024-10-21 is the version both UGM (its SDK default) and UAI were sending. From now
            // on IT Administrator sets it per model in AI Setup.
            migrationBuilder.Sql(
                "UPDATE \"AI\".\"AIModels\" SET \"ApiVersion\" = '2024-10-21' WHERE \"ApiVersion\" = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiVersion",
                schema: "AI",
                table: "AIModels");
        }
    }
}
