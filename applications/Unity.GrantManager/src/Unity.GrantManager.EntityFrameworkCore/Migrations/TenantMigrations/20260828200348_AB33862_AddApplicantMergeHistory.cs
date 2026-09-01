using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AB33862_AddApplicantMergeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicantMergeOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrincipalApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecondaryApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PrincipalStateBefore = table.Column<string>(type: "jsonb", nullable: false),
                    PrincipalStateAfter = table.Column<string>(type: "jsonb", nullable: false),
                    SecondaryStateBefore = table.Column<string>(type: "jsonb", nullable: false),
                    SecondaryStateAfter = table.Column<string>(type: "jsonb", nullable: false),
                    MergedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MergedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReversedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversalReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SnapshotVersion = table.Column<int>(type: "integer", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantMergeOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicantMergeOperations_Applicants_PrincipalApplicantId",
                        column: x => x.PrincipalApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApplicantMergeOperations_Applicants_SecondaryApplicantId",
                        column: x => x.SecondaryApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApplicantMergeApplicationChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApplicantMergeOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WasTransferred = table.Column<bool>(type: "boolean", nullable: false),
                    ApplicantIdBefore = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantIdAfter = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultSiteIdBefore = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultSiteIdAfter = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedRecordsSnapshot = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantMergeApplicationChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicantMergeApplicationChanges_ApplicantMergeOperations_A~",
                        column: x => x.ApplicantMergeOperationId,
                        principalTable: "ApplicantMergeOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicantMergeApplicationChanges_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantMergeApplicationChanges_ApplicantMergeOperationId_~",
                table: "ApplicantMergeApplicationChanges",
                columns: new[] { "ApplicantMergeOperationId", "ApplicationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantMergeApplicationChanges_ApplicationId",
                table: "ApplicantMergeApplicationChanges",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantMergeApplicationChanges_TenantId_ApplicationId",
                table: "ApplicantMergeApplicationChanges",
                columns: new[] { "TenantId", "ApplicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantMergeOperations_PrincipalApplicantId",
                table: "ApplicantMergeOperations",
                column: "PrincipalApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantMergeOperations_SecondaryApplicantId",
                table: "ApplicantMergeOperations",
                column: "SecondaryApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantMergeOperations_TenantId_MergedAt",
                table: "ApplicantMergeOperations",
                columns: new[] { "TenantId", "MergedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantMergeOperations_TenantId_PrincipalApplicantId_Stat~",
                table: "ApplicantMergeOperations",
                columns: new[] { "TenantId", "PrincipalApplicantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantMergeOperations_TenantId_SecondaryApplicantId_Stat~",
                table: "ApplicantMergeOperations",
                columns: new[] { "TenantId", "SecondaryApplicantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicantMergeApplicationChanges");

            migrationBuilder.DropTable(
                name: "ApplicantMergeOperations");
        }
    }
}
