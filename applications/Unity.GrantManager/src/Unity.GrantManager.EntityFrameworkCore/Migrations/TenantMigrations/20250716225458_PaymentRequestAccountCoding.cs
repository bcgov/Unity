using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class PaymentRequestAccountCoding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountCodingId",
                table: "PaymentRequests",
                type: "uuid",
                nullable: false,
                schema: "Payments",
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));


            // Need to run some scripts to move existing  Payment configurations to AccountCodings then relate the fk to the above column

            // migrationBuilder.AddForeignKey(
            //     name: "FK_PaymentRequest_AccountCodings_Id",
            //     schema: "Payments",
            //     table: "PaymentRequests",
            //     column: "AccountCodingId",
            //     principalSchema: "Payments",
            //     principalTable: "AccountCodings",
            //     principalColumn: "Id");               

            // migrationBuilder.DropColumn(
            //          name: "PaymentThreshold",
            //          table: "PaymentConfigurations",
            //          schema: "Payments");

            // migrationBuilder.DropColumn(
            //     name: "MinistryClient",
            //     table: "PaymentConfigurations",
            //     schema: "Payments");

            // migrationBuilder.DropColumn(
            //     name: "Responsibility",
            //     table: "PaymentConfigurations",
            //     schema: "Payments");

            // migrationBuilder.DropColumn(
            //     name: "ServiceLine",
            //     table: "PaymentConfigurations",
            //     schema: "Payments");

            // migrationBuilder.DropColumn(
            //     name: "Stob",
            //     table: "PaymentConfigurations",
            //     schema: "Payments");

            // migrationBuilder.DropColumn(
            //     name: "ProjectNumber",
            //     table: "PaymentConfigurations",
            //     schema: "Payments");                       

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            // migrationBuilder.DropForeignKey(
            //     schema: "Payments",
            //     name: "FK_ApplicationForms_AccountCodings_Id",
            //     table: "PaymentRequests");

            migrationBuilder.DropColumn(
                     name: "AccountCodingId",
                     table: "PaymentRequests",
                     schema: "Payments");
        }
    }
}
