using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationStatusTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApplicationName",
                table: "UnityApplication",
                newName: "ProjectName");

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationStatusId",
                table: "UnityApplication",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<double>(
                name: "EligibleAmount",
                table: "UnityApplication",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ProposalDate",
                table: "UnityApplication",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNo",
                table: "UnityApplication",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "RequestedAmount",
                table: "UnityApplication",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "SubmissionDate",
                table: "UnityApplication",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateTable(
                name: "UnityApplicationStatus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalStatus = table.Column<string>(type: "text", nullable: false),
                    InternalStatus = table.Column<string>(type: "text", nullable: false),
                    StatusCode = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnityApplicationStatus", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplication_ApplicationStatusId",
                table: "UnityApplication",
                column: "ApplicationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationStatus_StatusCode",
                table: "UnityApplicationStatus",
                column: "StatusCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UnityApplication_UnityApplicationStatus_ApplicationStatusId",
                table: "UnityApplication",
                column: "ApplicationStatusId",
                principalTable: "UnityApplicationStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnityApplication_UnityApplicationStatus_ApplicationStatusId",
                table: "UnityApplication");

            migrationBuilder.DropTable(
                name: "UnityApplicationStatus");

            migrationBuilder.DropIndex(
                name: "IX_UnityApplication_ApplicationStatusId",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "ApplicationStatusId",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "EligibleAmount",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "ProposalDate",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "ReferenceNo",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "RequestedAmount",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "SubmissionDate",
                table: "UnityApplication");

            migrationBuilder.RenameColumn(
                name: "ProjectName",
                table: "UnityApplication",
                newName: "ApplicationName");
        }
    }
}
