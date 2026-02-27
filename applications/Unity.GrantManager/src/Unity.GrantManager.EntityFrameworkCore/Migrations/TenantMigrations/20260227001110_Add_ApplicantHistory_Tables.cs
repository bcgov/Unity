using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Add_ApplicantHistory_Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuditComments",
                table: "Applicants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundingHistoryComments",
                table: "Applicants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssueTrackingComments",
                table: "Applicants",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuditTrackingNumber = table.Column<string>(type: "text", nullable: true),
                    AuditDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AuditNote = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_AuditHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditHistories_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FundingHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrantCategory = table.Column<string>(type: "text", nullable: true),
                    FundingYear = table.Column<int>(type: "integer", nullable: true),
                    RenewedFunding = table.Column<bool>(type: "boolean", nullable: true),
                    ApprovedAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    ReconsiderationAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalGrantAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    FundingNotes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_FundingHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundingHistories_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IssueTrackings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    IssueHeading = table.Column<string>(type: "text", nullable: true),
                    IssueDescription = table.Column<string>(type: "text", nullable: true),
                    Resolved = table.Column<bool>(type: "boolean", nullable: true),
                    ResolutionNote = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_IssueTrackings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueTrackings_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditHistories_ApplicantId",
                table: "AuditHistories",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_FundingHistories_ApplicantId",
                table: "FundingHistories",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueTrackings_ApplicantId",
                table: "IssueTrackings",
                column: "ApplicantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditHistories");

            migrationBuilder.DropTable(
                name: "FundingHistories");

            migrationBuilder.DropTable(
                name: "IssueTrackings");

            migrationBuilder.DropColumn(
                name: "AuditComments",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "FundingHistoryComments",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "IssueTrackingComments",
                table: "Applicants");
        }
    }
}
