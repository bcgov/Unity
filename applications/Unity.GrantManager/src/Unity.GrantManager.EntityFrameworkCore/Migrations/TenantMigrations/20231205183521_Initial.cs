using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Applicant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    NonRegisteredBusinessName = table.Column<string>(type: "text", nullable: true),
                    OrgName = table.Column<string>(type: "text", nullable: true),
                    OrgNumber = table.Column<string>(type: "text", nullable: true),
                    OrgStatus = table.Column<string>(type: "text", nullable: true),
                    OrganizationType = table.Column<string>(type: "text", nullable: true),
                    Sector = table.Column<string>(type: "text", nullable: true),
                    SubSector = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    ApproxNumberOfEmployees = table.Column<string>(type: "text", nullable: true),
                    EconomicRegion = table.Column<string>(type: "text", nullable: true),
                    Community = table.Column<string>(type: "text", nullable: true),
                    IndigenousOrgInd = table.Column<string>(type: "text", nullable: true),
                    ElectoralDistrict = table.Column<string>(type: "text", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applicant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationStatus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalStatus = table.Column<string>(type: "text", nullable: false),
                    InternalStatus = table.Column<string>(type: "text", nullable: false),
                    StatusCode = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GrantApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrantApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Intake",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Budget = table.Column<double>(type: "double precision", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IntakeName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
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
                    table.PrimaryKey("PK_Intake", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OidcSub = table.Column<string>(type: "text", nullable: false),
                    OidcDisplayName = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Badge = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.UniqueConstraint("AK_User_OidcSub", x => x.OidcSub);
                });

            migrationBuilder.CreateTable(
                name: "Address",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Province = table.Column<string>(type: "text", nullable: true),
                    Postal = table.Column<string>(type: "text", nullable: true),
                    Street = table.Column<string>(type: "text", nullable: true),
                    Street2 = table.Column<string>(type: "text", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Address_Applicant_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationForm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntakeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationFormName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ApplicationFormDescription = table.Column<string>(type: "text", nullable: true),
                    ChefsApplicationFormGuid = table.Column<string>(type: "text", nullable: true),
                    ChefsFormVersionGuid = table.Column<string>(type: "text", nullable: true),
                    ChefsCriteriaFormGuid = table.Column<string>(type: "text", nullable: true),
                    ApiKey = table.Column<string>(type: "text", nullable: true),
                    SubmissionHeaderMapping = table.Column<string>(type: "text", nullable: true),
                    AvailableChefsFields = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_ApplicationForm", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationForm_Intake_IntakeId",
                        column: x => x.IntakeId,
                        principalTable: "Intake",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicantAgent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OidcSubUser = table.Column<string>(type: "text", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RoleForApplicant = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ContactOrder = table.Column<int>(type: "integer", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Phone2 = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantAgent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicantAgent_Applicant_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicantAgent_User_OidcSubUser",
                        column: x => x.OidcSubUser,
                        principalTable: "User",
                        principalColumn: "OidcSub",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Application",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationStatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ReferenceNo = table.Column<string>(type: "text", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalProjectBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    Sector = table.Column<string>(type: "text", nullable: true),
                    EconomicRegion = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    ProposalDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AssessmentStartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FinalDecisionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    ProjectSummary = table.Column<string>(type: "text", nullable: true),
                    TotalScore = table.Column<int>(type: "integer", nullable: true),
                    RecommendedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    LikelihoodOfFunding = table.Column<string>(type: "text", nullable: true),
                    DueDilligenceStatus = table.Column<string>(type: "text", nullable: true),
                    Recommendation = table.Column<string>(type: "text", nullable: true),
                    DeclineRational = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    AssessmentResultStatus = table.Column<string>(type: "text", nullable: true),
                    AssessmentResultDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Application", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Application_Applicant_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Application_ApplicationForm_ApplicationFormId",
                        column: x => x.ApplicationFormId,
                        principalTable: "ApplicationForm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Application_ApplicationStatus_ApplicationStatusId",
                        column: x => x.ApplicationStatusId,
                        principalTable: "ApplicationStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationFormSubmission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OidcSub = table.Column<string>(type: "text", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChefsSubmissionGuid = table.Column<string>(type: "text", nullable: false),
                    Submission = table.Column<string>(type: "jsonb", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationFormSubmission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationFormSubmission_Applicant_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationFormSubmission_ApplicationForm_ApplicationFormId",
                        column: x => x.ApplicationFormId,
                        principalTable: "ApplicationForm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationAttachment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    S3ObjectKey = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    AttachedBy = table.Column<string>(type: "text", nullable: true),
                    Time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationAttachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationAttachment_Application_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Application",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationComment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    CommenterId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationComment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationComment_Application_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Application",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationComment_User_CommenterId",
                        column: x => x.CommenterId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationUserAssignment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUserAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationUserAssignment_Application_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Application",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationUserAssignment_User_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Assessment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessorId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalRecommended = table.Column<bool>(type: "boolean", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FinancialAnalysis = table.Column<int>(type: "integer", nullable: true),
                    EconomicImpact = table.Column<int>(type: "integer", nullable: true),
                    InclusiveGrowth = table.Column<int>(type: "integer", nullable: true),
                    CleanGrowth = table.Column<int>(type: "integer", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assessment_Application_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Application",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Assessment_User_AssessorId",
                        column: x => x.AssessorId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssessmentAttachment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    S3ObjectKey = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    AttachedBy = table.Column<string>(type: "text", nullable: true),
                    Time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentAttachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentAttachment_Assessment_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentComment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    CommenterId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentComment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentComment_Assessment_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentComment_User_CommenterId",
                        column: x => x.CommenterId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Address_ApplicantId",
                table: "Address",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_Applicant_ApplicantName",
                table: "Applicant",
                column: "ApplicantName");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantAgent_ApplicantId",
                table: "ApplicantAgent",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantAgent_OidcSubUser",
                table: "ApplicantAgent",
                column: "OidcSubUser");

            migrationBuilder.CreateIndex(
                name: "IX_Application_ApplicantId",
                table: "Application",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_Application_ApplicationFormId",
                table: "Application",
                column: "ApplicationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_Application_ApplicationStatusId",
                table: "Application",
                column: "ApplicationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationAttachment_ApplicationId",
                table: "ApplicationAttachment",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationComment_ApplicationId",
                table: "ApplicationComment",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationComment_CommenterId",
                table: "ApplicationComment",
                column: "CommenterId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationForm_IntakeId",
                table: "ApplicationForm",
                column: "IntakeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationFormSubmission_ApplicantId",
                table: "ApplicationFormSubmission",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationFormSubmission_ApplicationFormId",
                table: "ApplicationFormSubmission",
                column: "ApplicationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatus_StatusCode",
                table: "ApplicationStatus",
                column: "StatusCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserAssignment_ApplicationId",
                table: "ApplicationUserAssignment",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserAssignment_AssigneeId",
                table: "ApplicationUserAssignment",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessment_ApplicationId",
                table: "Assessment",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessment_AssessorId",
                table: "Assessment",
                column: "AssessorId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttachment_AssessmentId",
                table: "AssessmentAttachment",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentComment_AssessmentId",
                table: "AssessmentComment",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentComment_CommenterId",
                table: "AssessmentComment",
                column: "CommenterId");

            migrationBuilder.CreateIndex(
                name: "IX_User_OidcSub",
                table: "User",
                column: "OidcSub");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Address");

            migrationBuilder.DropTable(
                name: "ApplicantAgent");

            migrationBuilder.DropTable(
                name: "ApplicationAttachment");

            migrationBuilder.DropTable(
                name: "ApplicationComment");

            migrationBuilder.DropTable(
                name: "ApplicationFormSubmission");

            migrationBuilder.DropTable(
                name: "ApplicationUserAssignment");

            migrationBuilder.DropTable(
                name: "AssessmentAttachment");

            migrationBuilder.DropTable(
                name: "AssessmentComment");

            migrationBuilder.DropTable(
                name: "GrantApplications");

            migrationBuilder.DropTable(
                name: "Assessment");

            migrationBuilder.DropTable(
                name: "Application");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Applicant");

            migrationBuilder.DropTable(
                name: "ApplicationForm");

            migrationBuilder.DropTable(
                name: "ApplicationStatus");

            migrationBuilder.DropTable(
                name: "Intake");
        }
    }
}
