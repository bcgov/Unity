using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class AddAIModelType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "AI",
                table: "AIModels",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Chat");

            // Every model before this migration is a chat model. Names are also made unique
            // ignoring case (EF can't model an index on lower("Name"), so it is raw SQL);
            // AISetupManager turns a hit on it into the duplicate-name error.
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"IX_AIModels_Name_Lower\" ON \"AI\".\"AIModels\" (lower(\"Name\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"AI\".\"IX_AIModels_Name_Lower\";");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "AI",
                table: "AIModels");
        }
    }
}
