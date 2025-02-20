﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddPaymentsDecisionUserAndSubmissionNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubmissionConfirmationCode",
                schema: "Payments",
                table: "PaymentRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DecisionUserId",
                schema: "Payments",
                table: "ExpenseApprovals",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Payments\".\"ExpenseApprovals\" SET \"DecisionUserId\" = \"LastModifierId\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmissionConfirmationCode",
                schema: "Payments",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "DecisionUserId",
                schema: "Payments",
                table: "ExpenseApprovals");
        }
    }
}
