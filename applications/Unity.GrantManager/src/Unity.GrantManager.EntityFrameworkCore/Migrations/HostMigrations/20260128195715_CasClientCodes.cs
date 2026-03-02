using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class CasClientCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CasClientCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),                                        
                    Description = table.Column<string>(type: "text", nullable: false),
                    MinistryPrefix = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    FinancialMinistry = table.Column<string>(type: "text", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    ClientId = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastUpdatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_CasClientCodes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CasClientCodes");
        }
    }
}
