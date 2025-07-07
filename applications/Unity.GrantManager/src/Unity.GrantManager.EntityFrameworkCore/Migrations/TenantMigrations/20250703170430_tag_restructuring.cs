using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class tag_restructuring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TagId",
                schema: "Payments",
                table: "PaymentTags",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
        }
    }
}
