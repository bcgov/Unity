using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class PaymentTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
               name: "PaymentTags",
               schema: "Payments",
               columns: table => new
               {
                   Id = table.Column<Guid>(type: "uuid", nullable: false),
                   TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                   PaymentRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                   Text = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                   ExtraProperties = table.Column<string>(type: "text", nullable: false),
                   ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                   CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                   CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                   LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                   LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                   IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                   DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                   DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                   
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_PaymentTags", x => x.Id);
               });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentTags",
                schema: "Payments");
        }
    }
}
