using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationFormVersionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnityApplicationFormVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChefsApplicationFormGuid = table.Column<string>(type: "text", nullable: true),
                    ChefsFormVersionGuid = table.Column<string>(type: "text", nullable: true),
                    SubmissionHeaderMapping = table.Column<string>(type: "text", nullable: true),
                    AvailableChefsFields = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: true),
                    Published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_UnityApplicationFormVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnityApplicationFormVersion_UnityApplicationForm_Applicatio~",
                        column: x => x.ApplicationFormId,
                        principalTable: "UnityApplicationForm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationFormVersion_ApplicationFormId",
                table: "UnityApplicationFormVersion",
                column: "ApplicationFormId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnityApplicationFormVersion");

        }
    }
}
