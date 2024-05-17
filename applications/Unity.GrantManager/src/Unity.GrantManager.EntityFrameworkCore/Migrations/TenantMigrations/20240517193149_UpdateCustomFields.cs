using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class UpdateCustomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomFieldValues_CustomFields_CustomFieldId",
                schema: "Flex",
                table: "CustomFieldValues");

            migrationBuilder.DropForeignKey(
                name: "FK_WorksheetInstances_Worksheets_WorksheetId",
                schema: "Flex",
                table: "WorksheetInstances");

            migrationBuilder.DropIndex(
                name: "IX_WorksheetInstances_WorksheetId",
                schema: "Flex",
                table: "WorksheetInstances");

            migrationBuilder.DropIndex(
                name: "IX_CustomFieldValues_CustomFieldId",
                schema: "Flex",
                table: "CustomFieldValues");

            migrationBuilder.DropColumn(
                name: "CorrelationProvider",
                schema: "Flex",
                table: "CustomFieldValues");

            migrationBuilder.DropColumn(
                name: "DefaultValue",
                schema: "Flex",
                table: "CustomFieldValues");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "Flex",
                table: "CustomFieldValues");

            migrationBuilder.RenameColumn(
                name: "Value",
                schema: "Flex",
                table: "WorksheetInstances",
                newName: "UiAnchor");

            migrationBuilder.RenameColumn(
                name: "CorrelationId",
                schema: "Flex",
                table: "CustomFieldValues",
                newName: "WorksheetInstanceId");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "Flex",
                table: "WorksheetSections",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Order",
                schema: "Flex",
                table: "WorksheetSections",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "CurrentValue",
                schema: "Flex",
                table: "WorksheetInstances",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AlterColumn<string>(
                name: "CurrentValue",
                schema: "Flex",
                table: "CustomFieldValues",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Definition",
                schema: "Flex",
                table: "CustomFields",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<long>(
                name: "Order",
                schema: "Flex",
                table: "CustomFields",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                schema: "Flex",
                table: "CustomFields",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldValues_WorksheetInstanceId",
                schema: "Flex",
                table: "CustomFieldValues",
                column: "WorksheetInstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomFieldValues_WorksheetInstances_WorksheetInstanceId",
                schema: "Flex",
                table: "CustomFieldValues",
                column: "WorksheetInstanceId",
                principalSchema: "Flex",
                principalTable: "WorksheetInstances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomFieldValues_WorksheetInstances_WorksheetInstanceId",
                schema: "Flex",
                table: "CustomFieldValues");

            migrationBuilder.DropIndex(
                name: "IX_CustomFieldValues_WorksheetInstanceId",
                schema: "Flex",
                table: "CustomFieldValues");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "Flex",
                table: "WorksheetSections");

            migrationBuilder.DropColumn(
                name: "Order",
                schema: "Flex",
                table: "WorksheetSections");

            migrationBuilder.DropColumn(
                name: "CurrentValue",
                schema: "Flex",
                table: "WorksheetInstances");

            migrationBuilder.DropColumn(
                name: "Definition",
                schema: "Flex",
                table: "CustomFields");

            migrationBuilder.DropColumn(
                name: "Order",
                schema: "Flex",
                table: "CustomFields");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "Flex",
                table: "CustomFields");

            migrationBuilder.RenameColumn(
                name: "UiAnchor",
                schema: "Flex",
                table: "WorksheetInstances",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "WorksheetInstanceId",
                schema: "Flex",
                table: "CustomFieldValues",
                newName: "CorrelationId");

            migrationBuilder.AlterColumn<string>(
                name: "CurrentValue",
                schema: "Flex",
                table: "CustomFieldValues",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<string>(
                name: "CorrelationProvider",
                schema: "Flex",
                table: "CustomFieldValues",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DefaultValue",
                schema: "Flex",
                table: "CustomFieldValues",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                schema: "Flex",
                table: "CustomFieldValues",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_WorksheetInstances_WorksheetId",
                schema: "Flex",
                table: "WorksheetInstances",
                column: "WorksheetId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldValues_CustomFieldId",
                schema: "Flex",
                table: "CustomFieldValues",
                column: "CustomFieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomFieldValues_CustomFields_CustomFieldId",
                schema: "Flex",
                table: "CustomFieldValues",
                column: "CustomFieldId",
                principalSchema: "Flex",
                principalTable: "CustomFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorksheetInstances_Worksheets_WorksheetId",
                schema: "Flex",
                table: "WorksheetInstances",
                column: "WorksheetId",
                principalSchema: "Flex",
                principalTable: "Worksheets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
