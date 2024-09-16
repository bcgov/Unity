using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.ApplicantPortal.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Applicants",
                columns: table => new
                {
                    ApplicantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OrganizationNumber = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    OrganizationSize = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrganizationBookStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrganizationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrganizationOperationStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OrganizationFiscalYearEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    OrganizationSector = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrganizationSubSector = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrganizationSocietyNumber = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    OrganizationBusinessLicenseNumber = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    PhysicalAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MailingAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applicants", x => x.ApplicantId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Applicants");
        }
    }
}
