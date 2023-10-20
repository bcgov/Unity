using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class Address : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnityAddress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: true),
                    City = table.Column<string>(type: "character varying(500)", nullable: true),
                    Country = table.Column<string>(type: "character varying(500)", nullable: true),
                    Province = table.Column<string>(type: "character varying(500)", nullable: true),
                    Postal = table.Column<string>(type: "character varying(50)", nullable: true),
                    Street = table.Column<string>(type: "character varying(2000)", nullable: true),
                    Street2 = table.Column<string>(type: "character varying(2000)", nullable: true),
                    Unit = table.Column<string>(type: "character varying(1000)", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnityAddress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnityAddress_UnityApplicant_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "UnityApplicant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnityAddress_ApplicantId",
                table: "UnityAddress",
                column: "ApplicantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnityAddress");
        }
    }
}
