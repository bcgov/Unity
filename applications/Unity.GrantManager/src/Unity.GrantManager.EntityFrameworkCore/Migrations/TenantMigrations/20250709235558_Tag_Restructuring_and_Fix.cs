using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Tag_Restructuring_and_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Text",
                schema: "Payments",
                table: "PaymentTags",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(250)",
                oldMaxLength: 250);

            migrationBuilder.AddColumn<Guid>(
                name: "TagId",
                schema: "Payments",
                table: "PaymentTags",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "ApplicationTags",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(250)",
                oldMaxLength: 250);

            migrationBuilder.AddColumn<Guid>(
                name: "TagId",
                table: "ApplicationTags",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            // 1. Flatten and insert unique tags from PaymentTags.Text and ApplicationTags.Text
            migrationBuilder.Sql(@"
                INSERT INTO ""Tags"" (""Id"", ""TenantId"", ""Name"", ""ExtraProperties"", ""ConcurrencyStamp"", ""CreationTime"")
                SELECT gen_random_uuid(), src.""TenantId"", src.""Name"", '', '', NOW()
                FROM (
                    SELECT DISTINCT ""TenantId"", TRIM(tag) AS ""Name""
                    FROM (
                        SELECT ""TenantId"", unnest(string_to_array(""Text"", ',')) AS tag
                        FROM ""Payments"".""PaymentTags""
                        UNION ALL
                        SELECT ""TenantId"", unnest(string_to_array(""Text"", ',')) AS tag
                        FROM ""ApplicationTags""
                    ) AS all_tags
                    WHERE TRIM(tag) <> ''
                ) AS src
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Tags"" t
                    WHERE t.""TenantId"" IS NOT DISTINCT FROM src.""TenantId""
                      AND t.""Name"" = src.""Name""
                );
            ");

            // 2. Normalize PaymentTags: create a new row for each tag in the Text column
            migrationBuilder.Sql(@"
                WITH flattened AS (
                    SELECT
                        pt.""Id"" AS ""OldId"",
                        pt.""PaymentRequestId"",
                        pt.""TenantId"",
                        TRIM(tag) AS ""TagName"",
                        pt.""Text"",
                        pt.""ExtraProperties"",
                        pt.""ConcurrencyStamp"",
                        pt.""CreationTime"",
                        pt.""CreatorId"",
                        pt.""LastModificationTime"",
                        pt.""LastModifierId""
                    FROM ""Payments"".""PaymentTags"" pt,
                    unnest(string_to_array(pt.""Text"", ',')) AS tag
                    WHERE TRIM(tag) <> ''
                )
                INSERT INTO ""Payments"".""PaymentTags"" (
                    ""Id"", ""PaymentRequestId"", ""TenantId"", ""TagId"", ""Text"", ""ExtraProperties"", ""ConcurrencyStamp"", ""CreationTime"", ""CreatorId"", ""LastModificationTime"", ""LastModifierId""
                )
                SELECT
                    gen_random_uuid(),
                    f.""PaymentRequestId"",
                    f.""TenantId"",
                    t.""Id"",
                    f.""TagName"",
                    f.""ExtraProperties"",
                    f.""ConcurrencyStamp"",
                    f.""CreationTime"",
                    f.""CreatorId"",
                    f.""LastModificationTime"",
                    f.""LastModifierId""
                FROM flattened f
                JOIN ""Tags"" t
                  ON t.""TenantId"" IS NOT DISTINCT FROM f.""TenantId""
                 AND t.""Name"" = f.""TagName"";
            ");

            // 3. Normalize ApplicationTags: create a new row for each tag in the Text column
            migrationBuilder.Sql(@"
                WITH flattened AS (
                    SELECT
                        at.""Id"" AS ""OldId"",
                        at.""ApplicationId"",
                        at.""TenantId"",
                        TRIM(tag) AS ""TagName"",
                        at.""Text"",
                        at.""ExtraProperties"",
                        at.""ConcurrencyStamp"",
                        at.""CreationTime"",
                        at.""CreatorId"",
                        at.""LastModificationTime"",
                        at.""LastModifierId""
                    FROM ""ApplicationTags"" at,
                    unnest(string_to_array(at.""Text"", ',')) AS tag
                    WHERE TRIM(tag) <> ''
                )
                INSERT INTO ""ApplicationTags"" (
                    ""Id"", ""ApplicationId"", ""TenantId"", ""TagId"", ""Text"", ""ExtraProperties"", ""ConcurrencyStamp"", ""CreationTime"", ""CreatorId"", ""LastModificationTime"", ""LastModifierId""
                )
                SELECT
                    gen_random_uuid(),
                    f.""ApplicationId"",
                    f.""TenantId"",
                    t.""Id"",
                    f.""TagName"",
                    f.""ExtraProperties"",
                    f.""ConcurrencyStamp"",
                    f.""CreationTime"",
                    f.""CreatorId"",
                    f.""LastModificationTime"",
                    f.""LastModifierId""
                FROM flattened f
                JOIN ""Tags"" t
                  ON t.""TenantId"" IS NOT DISTINCT FROM f.""TenantId""
                 AND t.""Name"" = f.""TagName"";
            ");

            // 4. Remove old (unnormalized) rows
            migrationBuilder.Sql(@"DELETE FROM ""Payments"".""PaymentTags"" WHERE POSITION(',' IN ""Text"") > 0;");
            migrationBuilder.Sql(@"DELETE FROM ""ApplicationTags"" WHERE POSITION(',' IN ""Text"") > 0;");

            // 5. After normalization, remove duplicate tag rows
            migrationBuilder.Sql(@"DELETE FROM ""Payments"".""PaymentTags"" WHERE ""TagId"" = '00000000-0000-0000-0000-000000000000';");
            migrationBuilder.Sql(@"DELETE FROM ""ApplicationTags"" WHERE ""TagId"" = '00000000-0000-0000-0000-000000000000';");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTags_TagId",
                schema: "Payments",
                table: "PaymentTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationTags_TagId",
                table: "ApplicationTags",
                column: "TagId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationTags_Tags_TagId",
                table: "ApplicationTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTags_Tags_TagId",
                schema: "Payments",
                table: "PaymentTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationTags_Tags_TagId",
                table: "ApplicationTags");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTags_Tags_TagId",
                schema: "Payments",
                table: "PaymentTags");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTags_TagId",
                schema: "Payments",
                table: "PaymentTags");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationTags_TagId",
                table: "ApplicationTags");

            migrationBuilder.DropColumn(
                name: "TagId",
                schema: "Payments",
                table: "PaymentTags");

            migrationBuilder.DropColumn(
                name: "TagId",
                table: "ApplicationTags");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                schema: "Payments",
                table: "PaymentTags",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "ApplicationTags",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
