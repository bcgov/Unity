using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class ApplicationRelationUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationAssignments_Applications_ApplicationId",
                table: "ApplicationAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationAssignments_Persons_AssigneeId",
                table: "ApplicationAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Applicants_ApplicantId",
                table: "Applications");

            migrationBuilder.DropForeignKey(
                name: "FK_Applications_ApplicationForms_ApplicationFormId",
                table: "Applications");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationTags_ApplicationId",
                table: "ApplicationTags",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_OwnerId",
                table: "Applications",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantAgents_ApplicationId",
                table: "ApplicantAgents",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicantAgents_Applications_ApplicationId",
                table: "ApplicantAgents",
                column: "ApplicationId",
                principalTable: "Applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationAssignments_Applications_ApplicationId",
                table: "ApplicationAssignments",
                column: "ApplicationId",
                principalTable: "Applications",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationAssignments_Persons_AssigneeId",
                table: "ApplicationAssignments",
                column: "AssigneeId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Applicants_ApplicantId",
                table: "Applications",
                column: "ApplicantId",
                principalTable: "Applicants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_ApplicationForms_ApplicationFormId",
                table: "Applications",
                column: "ApplicationFormId",
                principalTable: "ApplicationForms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Persons_OwnerId",
                table: "Applications",
                column: "OwnerId",
                principalTable: "Persons",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationTags_Applications_ApplicationId",
                table: "ApplicationTags",
                column: "ApplicationId",
                principalTable: "Applications",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicantAgents_Applications_ApplicationId",
                table: "ApplicantAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationAssignments_Applications_ApplicationId",
                table: "ApplicationAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationAssignments_Persons_AssigneeId",
                table: "ApplicationAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Applicants_ApplicantId",
                table: "Applications");

            migrationBuilder.DropForeignKey(
                name: "FK_Applications_ApplicationForms_ApplicationFormId",
                table: "Applications");

            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Persons_OwnerId",
                table: "Applications");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationTags_Applications_ApplicationId",
                table: "ApplicationTags");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationTags_ApplicationId",
                table: "ApplicationTags");

            migrationBuilder.DropIndex(
                name: "IX_Applications_OwnerId",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_ApplicantAgents_ApplicationId",
                table: "ApplicantAgents");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationAssignments_Applications_ApplicationId",
                table: "ApplicationAssignments",
                column: "ApplicationId",
                principalTable: "Applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationAssignments_Persons_AssigneeId",
                table: "ApplicationAssignments",
                column: "AssigneeId",
                principalTable: "Persons",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Applicants_ApplicantId",
                table: "Applications",
                column: "ApplicantId",
                principalTable: "Applicants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_ApplicationForms_ApplicationFormId",
                table: "Applications",
                column: "ApplicationFormId",
                principalTable: "ApplicationForms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
