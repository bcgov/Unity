using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAttachmentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "S3BucketId",
                table: "UnityAssessment");

            migrationBuilder.DropColumn(
                name: "S3Guid",
                table: "UnityApplicationAttachment");

            migrationBuilder.DropColumn(
                name: "S3BucketId",
                table: "UnityApplication");

            migrationBuilder.DropColumn(
                name: "S3Guid",
                table: "UnityAdjudicationAttachment");

            migrationBuilder.AddColumn<string>(
                name: "S3ObjectKey",
                table: "UnityApplicationAttachment",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "S3ObjectKey",
                table: "UnityAdjudicationAttachment",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "S3ObjectKey",
                table: "UnityApplicationAttachment");

            migrationBuilder.DropColumn(
                name: "S3ObjectKey",
                table: "UnityAdjudicationAttachment");

            migrationBuilder.AddColumn<Guid>(
                name: "S3BucketId",
                table: "UnityAssessment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "S3Guid",
                table: "UnityApplicationAttachment",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "S3BucketId",
                table: "UnityApplication",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "S3Guid",
                table: "UnityAdjudicationAttachment",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
