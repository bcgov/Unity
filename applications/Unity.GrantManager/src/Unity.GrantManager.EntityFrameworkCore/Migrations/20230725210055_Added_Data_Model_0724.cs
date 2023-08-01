using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class AddedDataModel0724 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnityIntake",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntakeName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnityIntake", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnityIntake_UnityGrantPrograms_GrantProgramId",
                        column: x => x.GrantProgramId,
                        principalTable: "UnityGrantPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnityGrantPrograms_ProgramName",
                table: "UnityGrantPrograms",
                column: "ProgramName");

            migrationBuilder.CreateIndex(
                name: "IX_UnityIntake_GrantProgramId",
                table: "UnityIntake",
                column: "GrantProgramId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnityIntake");

            migrationBuilder.DropIndex(
                name: "IX_UnityGrantPrograms_ProgramName",
                table: "UnityGrantPrograms");
        }
    }
}
