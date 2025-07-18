using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AccountCoding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountCodings",
                schema: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MinistryClient = table.Column<string>(type: "text", nullable: false),
                    Responsibility = table.Column<string>(type: "text", nullable: false),
                    ServiceLine = table.Column<string>(type: "text", nullable: false),
                    Stob = table.Column<string>(type: "text", nullable: false),
                    ProjectNumber = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountCodings", x => x.Id);
                    table.UniqueConstraint("UK_AccountCodings", x => new { x.MinistryClient, x.Responsibility, x.ServiceLine, x.Stob, x.ProjectNumber });
            });

            migrationBuilder.CreateTable(
                name: "PaymentThresholds",
                schema: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Threshold = table.Column<decimal>(type: "decimal", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentThresholds", x => x.Id);                                        
                }
            );

            migrationBuilder.AddColumn<string>(
                    name: "DefaultAccountCodingId",
                    table: "PaymentConfigurations",
                    type: "uuid",
                    nullable: false,
                    schema: "Payments",
                    defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConfiguration_DefaultAccountCodingId",
                schema: "Payments",
                table: "PaymentConfigurations",
                column: "DefaultAccountCodingId");


            migrationBuilder.AddColumn<string>(
                    name: "PaymentApprovalThreshold",
                    table: "ApplicationForms",
                    type: "decimal",
                    nullable: true,
                    defaultValue: false);

            migrationBuilder.AddColumn<string>(
                    name: "PreventPayment",
                    table: "ApplicationForms",
                    type: "bool",
                    nullable: false,
                    defaultValue: false);

            migrationBuilder.AddColumn<string>(
                    name: "AccountCodingId",
                    table: "ApplicationForms",
                    type: "uuid",
                    nullable: true,
                    defaultValue: null);
            

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationForms_AccountCodingId",
                table: "ApplicationForms",
                column: "AccountCodingId");            

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationForms_AccountCodings_Id",
                table: "ApplicationForms");

            migrationBuilder.AddColumn<decimal>(
                name: "PaymentThreshold",
                schema: "Payments",
                table: "PaymentConfigurations",
                type: "numeric");

             migrationBuilder.DropColumn(
                     name: "PaymentThreshold",
                     table: "PaymentConfigurations",
                     schema: "Payments");

            migrationBuilder.DropColumn(
                    name: "PreventPayment",
                    table: "ApplicationForms");

            migrationBuilder.DropColumn(
                    name: "DefaultAccountCodingId",
                    table: "PaymentConfigurations",
                    schema: "Payments");

            migrationBuilder.DropColumn(
                    name: "AccountCodingId",
                    table: "ApplicationForms");   

            migrationBuilder.DropColumn(
                    name: "PaymentApprovalThreshold",
                    table: "ApplicationForms");   

            migrationBuilder.DropTable(
                schema: "Payments",
                name: "AccountCodings");

            migrationBuilder.DropTable(
                schema: "Payments",
                name: "PaymentThresholds");
        }
    }
}
