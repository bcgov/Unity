using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AB33327_MoveAIScoresheetAnswersToAISchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationScoresheetAnswers",
                schema: "AI",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Answers = table.Column<string>(type: "jsonb", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationScoresheetAnswers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationScoresheetAnswers_ApplicationId",
                schema: "AI",
                table: "ApplicationScoresheetAnswers",
                column: "ApplicationId",
                unique: true);

            // Carry existing AI scoring results across before the source column is dropped.
            // EF cannot scaffold this; without it every stored result is lost.
            migrationBuilder.Sql(@"
                INSERT INTO ""AI"".""ApplicationScoresheetAnswers""
                    (""Id"", ""ApplicationId"", ""Answers"", ""TenantId"",
                     ""CreationTime"", ""ExtraProperties"", ""ConcurrencyStamp"")
                SELECT gen_random_uuid(),
                       a.""Id"",
                       a.""AIScoresheetAnswers"",
                       a.""TenantId"",
                       now() AT TIME ZONE 'utc',
                       '{}',
                       replace(gen_random_uuid()::text, '-', '')
                FROM ""Applications"" a
                WHERE a.""AIScoresheetAnswers"" IS NOT NULL;
            ");

            migrationBuilder.DropColumn(
                name: "AIScoresheetAnswers",
                table: "Applications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AIScoresheetAnswers",
                table: "Applications",
                type: "jsonb",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""Applications"" a
                SET ""AIScoresheetAnswers"" = s.""Answers""
                FROM ""AI"".""ApplicationScoresheetAnswers"" s
                WHERE s.""ApplicationId"" = a.""Id"";
            ");

            migrationBuilder.DropTable(
                name: "ApplicationScoresheetAnswers",
                schema: "AI");
        }
    }
}
