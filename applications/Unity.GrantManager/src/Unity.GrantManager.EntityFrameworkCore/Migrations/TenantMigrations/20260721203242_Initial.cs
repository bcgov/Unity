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
            migrationBuilder.EnsureSchema(
                name: "Payments");

            migrationBuilder.EnsureSchema(
                name: "Flex");

            migrationBuilder.EnsureSchema(
                name: "Notifications");

            migrationBuilder.EnsureSchema(
                name: "Reporting");

            migrationBuilder.CreateTable(
                name: "AccountCodings",
                schema: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    MinistryClient = table.Column<string>(type: "text", nullable: false),
                    Responsibility = table.Column<string>(type: "text", nullable: false),
                    ServiceLine = table.Column<string>(type: "text", nullable: false),
                    Stob = table.Column<string>(type: "text", nullable: false),
                    ProjectNumber = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountCodings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Applicants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantName = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    NonRegisteredBusinessName = table.Column<string>(type: "text", nullable: true),
                    OrgName = table.Column<string>(type: "text", nullable: true),
                    OrgNumber = table.Column<string>(type: "text", nullable: true),
                    OrgStatus = table.Column<string>(type: "text", nullable: true),
                    OrganizationType = table.Column<string>(type: "text", nullable: true),
                    Sector = table.Column<string>(type: "text", nullable: true),
                    SubSector = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    ApproxNumberOfEmployees = table.Column<string>(type: "text", nullable: true),
                    IndigenousOrgInd = table.Column<string>(type: "text", nullable: true),
                    SectorSubSectorIndustryDesc = table.Column<string>(type: "text", nullable: true),
                    RedStop = table.Column<bool>(type: "boolean", nullable: true),
                    UnityApplicantId = table.Column<string>(type: "text", nullable: true),
                    FiscalMonth = table.Column<string>(type: "text", nullable: true),
                    BusinessNumber = table.Column<string>(type: "text", nullable: true),
                    FiscalDay = table.Column<int>(type: "integer", nullable: true),
                    StartedOperatingDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchPercentage = table.Column<decimal>(type: "numeric", nullable: true),
                    NonRegOrgName = table.Column<string>(type: "text", nullable: true),
                    IsDuplicated = table.Column<bool>(type: "boolean", nullable: false),
                    FundingHistoryComments = table.Column<string>(type: "text", nullable: true),
                    IssueTrackingComments = table.Column<string>(type: "text", nullable: true),
                    AuditComments = table.Column<string>(type: "text", nullable: true),
                    ReportsComments = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applicants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalStatus = table.Column<string>(type: "text", nullable: false),
                    InternalStatus = table.Column<string>(type: "text", nullable: false),
                    NotifiedStatus = table.Column<string>(type: "text", nullable: true),
                    StatusCode = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
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
                    table.PrimaryKey("PK_ApplicationStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    HomePhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MobilePhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WorkPhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WorkPhoneExtension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailGroups",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailLogs",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledNotificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromAddress = table.Column<string>(type: "text", nullable: false),
                    ToAddress = table.Column<string>(type: "text", nullable: false),
                    CC = table.Column<string>(type: "text", nullable: false),
                    BCC = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    BodyType = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    Tag = table.Column<string>(type: "text", nullable: false),
                    RetryAttempts = table.Column<int>(type: "integer", nullable: false),
                    ChesMsgId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChesResponse = table.Column<string>(type: "text", nullable: false),
                    ChesStatus = table.Column<string>(type: "text", nullable: false),
                    ChesHttpStatusCode = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Recipient = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    EmailType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SendOnDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SentDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TemplateName = table.Column<string>(type: "text", nullable: false),
                    PaymentRequestIds = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    BodyText = table.Column<string>(type: "text", nullable: false),
                    BodyHTML = table.Column<string>(type: "text", nullable: false),
                    SendFrom = table.Column<string>(type: "text", nullable: false),
                    RecipientCategory = table.Column<string>(type: "text", nullable: true),
                    RecipientIdentifier = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Intakes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Budget = table.Column<double>(type: "double precision", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IntakeName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_Intakes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentConfigurations",
                schema: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultAccountCodingId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentIdPrefix = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_PaymentConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentThresholds",
                schema: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Threshold = table.Column<decimal>(type: "numeric", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_PaymentThresholds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OidcSub = table.Column<string>(type: "text", nullable: false),
                    OidcDisplayName = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Badge = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_Persons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportColumnsMaps",
                schema: "Reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationProvider = table.Column<string>(type: "text", nullable: false),
                    Mapping = table.Column<string>(type: "jsonb", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ViewName = table.Column<string>(type: "text", nullable: false),
                    ViewStatus = table.Column<int>(type: "integer", nullable: false),
                    RoleStatus = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportColumnsMaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledNotifications",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    FormId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationStatusId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApplicationStatus = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TriggerType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TriggerDetail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RecipientCategory = table.Column<string>(type: "text", nullable: true),
                    RecipientIdentifier = table.Column<string>(type: "text", nullable: true),
                    DateField = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
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
                    table.PrimaryKey("PK_ScheduledNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scoresheets",
                schema: "Flex",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Order = table.Column<long>(type: "bigint", nullable: false),
                    Published = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportColumns = table.Column<string>(type: "text", nullable: false),
                    ReportKeys = table.Column<string>(type: "text", nullable: false),
                    ReportViewName = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_Scoresheets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subscribers",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscribers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionGroups",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_SubscriptionGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Number = table.Column<string>(type: "text", nullable: true),
                    Subcategory = table.Column<string>(type: "text", nullable: true),
                    SIN = table.Column<string>(type: "text", nullable: true),
                    ProviderId = table.Column<string>(type: "text", nullable: true),
                    BusinessNumber = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    SupplierProtected = table.Column<string>(type: "text", nullable: true),
                    StandardIndustryClassification = table.Column<string>(type: "text", nullable: true),
                    LastUpdatedInCAS = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    MailingAddress = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Province = table.Column<string>(type: "text", nullable: true),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemplateVariables",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    MapTo = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateVariables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Triggers",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    InternalName = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Triggers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorksheetInstances",
                schema: "Flex",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentValue = table.Column<string>(type: "jsonb", nullable: false),
                    WorksheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationProvider = table.Column<string>(type: "text", nullable: false),
                    WorksheetCorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorksheetCorrelationProvider = table.Column<string>(type: "text", nullable: false),
                    UiAnchor = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportData = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_WorksheetInstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Worksheets",
                schema: "Flex",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Published = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportColumns = table.Column<string>(type: "text", nullable: false),
                    ReportKeys = table.Column<string>(type: "text", nullable: false),
                    ReportViewName = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_Worksheets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicantAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    S3ObjectKey = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicantAttachments_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuditTrackingNumber = table.Column<string>(type: "text", nullable: true),
                    AuditDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AuditStatus = table.Column<string>(type: "text", nullable: true),
                    AuditorName = table.Column<string>(type: "text", nullable: true),
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
                    FundingYear = table.Column<string>(type: "text", nullable: true),
                    RenewedFunding = table.Column<bool>(type: "boolean", nullable: true),
                    ApprovedAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    OneTimeConsideration = table.Column<decimal>(type: "numeric", nullable: true),
                    ReconsiderationAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalGrantAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    PaidDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
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
                    Year = table.Column<string>(type: "text", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "ReportsHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: true),
                    FiscalYear = table.Column<string>(type: "text", nullable: true),
                    ReportDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Outstanding = table.Column<bool>(type: "boolean", nullable: true),
                    SignedOff = table.Column<bool>(type: "boolean", nullable: true),
                    IncompleteReport = table.Column<bool>(type: "boolean", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_ReportsHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportsHistories_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContactLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_ContactLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactLinks_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailGroupUsers",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailGroupUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailGroupUsers_EmailGroups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "Notifications",
                        principalTable: "EmailGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailLogAttachments",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailLogId = table.Column<Guid>(type: "uuid", nullable: true),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    S3ObjectKey = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_EmailLogAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailLogAttachments_EmailLogs_EmailLogId",
                        column: x => x.EmailLogId,
                        principalSchema: "Notifications",
                        principalTable: "EmailLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailLogAttachments_EmailTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "Notifications",
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationForms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntakeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationFormName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ApplicationFormDescription = table.Column<string>(type: "text", nullable: true),
                    ChefsApplicationFormGuid = table.Column<string>(type: "text", nullable: true),
                    ChefsCriteriaFormGuid = table.Column<string>(type: "text", nullable: true),
                    ApiKey = table.Column<string>(type: "text", nullable: true),
                    AvailableChefsFields = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    ConnectionHttpStatus = table.Column<string>(type: "text", nullable: true),
                    AttemptedConnectionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Payable = table.Column<bool>(type: "boolean", nullable: false),
                    PreventPayment = table.Column<bool>(type: "boolean", nullable: false),
                    AccountCodingId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScoresheetId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentApprovalThreshold = table.Column<decimal>(type: "numeric", nullable: true),
                    DefaultPaymentGroup = table.Column<int>(type: "integer", nullable: true),
                    FormHierarchy = table.Column<int>(type: "integer", nullable: true),
                    ParentFormId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDirectApproval = table.Column<bool>(type: "boolean", nullable: false),
                    AutomaticallyGenerateAIAnalysis = table.Column<bool>(type: "boolean", nullable: false),
                    ManuallyInitiateAIAnalysis = table.Column<bool>(type: "boolean", nullable: false),
                    Prefix = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SuffixType = table.Column<int>(type: "integer", nullable: true),
                    ElectoralDistrictAddressType = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_ApplicationForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationForms_ApplicationForms_ParentFormId",
                        column: x => x.ParentFormId,
                        principalTable: "ApplicationForms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApplicationForms_Intakes_IntakeId",
                        column: x => x.IntakeId,
                        principalTable: "Intakes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicantComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    CommenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PinDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicantComments_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicantComments_Persons_CommenterId",
                        column: x => x.CommenterId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoresheetInstances",
                schema: "Flex",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    ScoresheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationProvider = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportData = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_ScoresheetInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoresheetInstances_Scoresheets_ScoresheetId",
                        column: x => x.ScoresheetId,
                        principalSchema: "Flex",
                        principalTable: "Scoresheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoresheetSections",
                schema: "Flex",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScoresheetId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ScoresheetSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoresheetSections_Scoresheets_ScoresheetId",
                        column: x => x.ScoresheetId,
                        principalSchema: "Flex",
                        principalTable: "Scoresheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionGroupSubscribers",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubscriberId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionGroupSubscribers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionGroupSubscribers_Subscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalSchema: "Notifications",
                        principalTable: "Subscribers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubscriptionGroupSubscribers_SubscriptionGroups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "Notifications",
                        principalTable: "SubscriptionGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                schema: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Number = table.Column<string>(type: "text", nullable: false),
                    PaymentGroup = table.Column<int>(type: "integer", nullable: false),
                    AddressLine1 = table.Column<string>(type: "text", nullable: true),
                    AddressLine2 = table.Column<string>(type: "text", nullable: true),
                    AddressLine3 = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Province = table.Column<string>(type: "text", nullable: true),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    EmailAddress = table.Column<string>(type: "text", nullable: true),
                    EFTAdvicePref = table.Column<string>(type: "text", nullable: true),
                    BankAccount = table.Column<string>(type: "text", nullable: true),
                    ProviderId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    SiteProtected = table.Column<string>(type: "text", nullable: true),
                    LastUpdatedInCas = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkDeletedInUse = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Sites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sites_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "Payments",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TriggerSubscriptions",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TriggerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TriggerSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TriggerSubscriptions_EmailTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "Notifications",
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TriggerSubscriptions_SubscriptionGroups_SubscriptionGroupId",
                        column: x => x.SubscriptionGroupId,
                        principalSchema: "Notifications",
                        principalTable: "SubscriptionGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TriggerSubscriptions_Triggers_TriggerId",
                        column: x => x.TriggerId,
                        principalSchema: "Notifications",
                        principalTable: "Triggers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomFieldValues",
                schema: "Flex",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentValue = table.Column<string>(type: "jsonb", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorksheetInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomFieldValues_WorksheetInstances_WorksheetInstanceId",
                        column: x => x.WorksheetInstanceId,
                        principalSchema: "Flex",
                        principalTable: "WorksheetInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorksheetLinks",
                schema: "Flex",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorksheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationProvider = table.Column<string>(type: "text", nullable: false),
                    UiAnchor = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<long>(type: "bigint", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorksheetLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorksheetLinks_Worksheets_WorksheetId",
                        column: x => x.WorksheetId,
                        principalSchema: "Flex",
                        principalTable: "Worksheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorksheetSections",
                schema: "Flex",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorksheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<long>(type: "bigint", nullable: false),
                    Definition = table.Column<string>(type: "jsonb", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_WorksheetSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorksheetSections_Worksheets_WorksheetId",
                        column: x => x.WorksheetId,
                        principalSchema: "Flex",
                        principalTable: "Worksheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationFormSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OidcSub = table.Column<string>(type: "text", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChefsSubmissionGuid = table.Column<string>(type: "text", nullable: false),
                    Submission = table.Column<string>(type: "jsonb", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    FormVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportData = table.Column<string>(type: "jsonb", nullable: false),
                    ApplicationFormVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationFormSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationFormSubmissions_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationFormSubmissions_ApplicationForms_ApplicationForm~",
                        column: x => x.ApplicationFormId,
                        principalTable: "ApplicationForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationFormVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChefsApplicationFormGuid = table.Column<string>(type: "text", nullable: true),
                    ChefsFormVersionGuid = table.Column<string>(type: "text", nullable: true),
                    SubmissionHeaderMapping = table.Column<string>(type: "text", nullable: true),
                    AvailableChefsFields = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: true),
                    Published = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportColumns = table.Column<string>(type: "text", nullable: false),
                    ReportKeys = table.Column<string>(type: "text", nullable: false),
                    ReportViewName = table.Column<string>(type: "text", nullable: false),
                    FormSchema = table.Column<string>(type: "jsonb", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationFormVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationFormVersion_ApplicationForms_ApplicationFormId",
                        column: x => x.ApplicationFormId,
                        principalTable: "ApplicationForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationStatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ReferenceNo = table.Column<string>(type: "text", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalProjectBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    EconomicRegion = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    ProposalDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AssessmentStartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FinalDecisionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NotificationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    ProjectSummary = table.Column<string>(type: "text", nullable: true),
                    TotalScore = table.Column<int>(type: "integer", nullable: true),
                    RecommendedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    LikelihoodOfFunding = table.Column<string>(type: "text", nullable: true),
                    DueDiligenceStatus = table.Column<string>(type: "text", nullable: true),
                    SubStatus = table.Column<string>(type: "text", nullable: true),
                    ExternalStatusVisibility = table.Column<bool>(type: "boolean", nullable: false),
                    DeclineRational = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    AssessmentResultStatus = table.Column<string>(type: "text", nullable: true),
                    AssessmentResultDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ProjectStartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ProjectEndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PercentageTotalProjectBudget = table.Column<double>(type: "double precision", nullable: true),
                    ProjectFundingTotal = table.Column<decimal>(type: "numeric", nullable: true),
                    Community = table.Column<string>(type: "text", nullable: true),
                    CommunityPopulation = table.Column<int>(type: "integer", nullable: true),
                    Acquisition = table.Column<string>(type: "text", nullable: true),
                    Forestry = table.Column<string>(type: "text", nullable: true),
                    ForestryFocus = table.Column<string>(type: "text", nullable: true),
                    ElectoralDistrict = table.Column<string>(type: "text", nullable: true),
                    ApplicantElectoralDistrict = table.Column<string>(type: "text", nullable: true),
                    Place = table.Column<string>(type: "text", nullable: true),
                    RegionalDistrict = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultSiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    SigningAuthorityFullName = table.Column<string>(type: "text", nullable: true),
                    SigningAuthorityTitle = table.Column<string>(type: "text", nullable: true),
                    SigningAuthorityEmail = table.Column<string>(type: "text", nullable: true),
                    SigningAuthorityBusinessPhone = table.Column<string>(type: "text", nullable: true),
                    SigningAuthorityCellPhone = table.Column<string>(type: "text", nullable: true),
                    ContractNumber = table.Column<string>(type: "text", nullable: true),
                    ContractExecutionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RiskRanking = table.Column<string>(type: "text", nullable: true),
                    UnityApplicationId = table.Column<string>(type: "text", nullable: true),
                    AIAnalysis = table.Column<string>(type: "text", nullable: true),
                    AIScoresheetAnswers = table.Column<string>(type: "jsonb", nullable: true),
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
                    table.PrimaryKey("PK_Applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Applications_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Applications_ApplicationForms_ApplicationFormId",
                        column: x => x.ApplicationFormId,
                        principalTable: "ApplicationForms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Applications_ApplicationStatuses_ApplicationStatusId",
                        column: x => x.ApplicationStatusId,
                        principalTable: "ApplicationStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Applications_Persons_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Persons",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                schema: "Flex",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Order = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Definition = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_ScoresheetSections_SectionId",
                        column: x => x.SectionId,
                        principalSchema: "Flex",
                        principalTable: "ScoresheetSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRequests",
                schema: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsRecon = table.Column<bool>(type: "boolean", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: false),
                    SubmissionConfirmationCode = table.Column<string>(type: "text", nullable: false),
                    InvoiceStatus = table.Column<string>(type: "text", nullable: true),
                    PaymentStatus = table.Column<string>(type: "text", nullable: true),
                    PaymentNumber = table.Column<string>(type: "text", nullable: true),
                    PaymentDate = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationProvider = table.Column<string>(type: "text", nullable: false),
                    PayeeName = table.Column<string>(type: "text", nullable: false),
                    ContractNumber = table.Column<string>(type: "text", nullable: false),
                    SupplierName = table.Column<string>(type: "text", nullable: true),
                    SupplierNumber = table.Column<string>(type: "text", nullable: false),
                    RequesterName = table.Column<string>(type: "text", nullable: false),
                    BatchName = table.Column<string>(type: "text", nullable: false),
                    BatchNumber = table.Column<decimal>(type: "numeric", nullable: false),
                    CasHttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    CasResponse = table.Column<string>(type: "text", nullable: true),
                    AccountCodingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    FsbNotificationEmailLogId = table.Column<Guid>(type: "uuid", nullable: true),
                    FsbNotificationSentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FsbApNotified = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CancelledOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CancelledById = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_PaymentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRequests_AccountCodings_AccountCodingId",
                        column: x => x.AccountCodingId,
                        principalSchema: "Payments",
                        principalTable: "AccountCodings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRequests_Sites_SiteId",
                        column: x => x.SiteId,
                        principalSchema: "Payments",
                        principalTable: "Sites",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomFields",
                schema: "Flex",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<long>(type: "bigint", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Definition = table.Column<string>(type: "jsonb", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_CustomFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomFields_WorksheetSections_SectionId",
                        column: x => x.SectionId,
                        principalSchema: "Flex",
                        principalTable: "WorksheetSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicantAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Province = table.Column<string>(type: "text", nullable: true),
                    Postal = table.Column<string>(type: "text", nullable: true),
                    Street = table.Column<string>(type: "text", nullable: true),
                    Street2 = table.Column<string>(type: "text", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    AddressType = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ApplicantAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicantAddresses_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApplicantAddresses_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApplicantAgents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OidcSubUser = table.Column<string>(type: "text", nullable: true),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RoleForApplicant = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ContactOrder = table.Column<int>(type: "integer", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Phone2 = table.Column<string>(type: "text", nullable: true),
                    PhoneExtension = table.Column<string>(type: "text", nullable: true),
                    Phone2Extension = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true),
                    BceidBusinessGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    BceidUserGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    BceidUserName = table.Column<string>(type: "text", nullable: true),
                    BceidBusinessName = table.Column<string>(type: "text", nullable: true),
                    IdentityName = table.Column<string>(type: "text", nullable: true),
                    IdentityEmail = table.Column<string>(type: "text", nullable: true),
                    IdentityProvider = table.Column<string>(type: "text", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantAgents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicantAgents_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicantAgents_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Duty = table.Column<string>(type: "text", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationAssignments_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApplicationAssignments_Persons_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationAttachments",
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
                    FileName = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    S3ObjectKey = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationAttachments_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationChefsFileAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChefsSubmissionId = table.Column<string>(type: "text", nullable: true),
                    ChefsFileId = table.Column<string>(type: "text", nullable: true),
                    AISummary = table.Column<string>(type: "text", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationChefsFileAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationChefsFileAttachments_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationComments",
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
                    CommenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PinDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationComments_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationComments_Persons_CommenterId",
                        column: x => x.CommenterId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationContact",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactType = table.Column<string>(type: "text", nullable: false),
                    ContactFullName = table.Column<string>(type: "text", nullable: false),
                    ContactTitle = table.Column<string>(type: "text", nullable: true),
                    ContactEmail = table.Column<string>(type: "text", nullable: true),
                    ContactMobilePhone = table.Column<string>(type: "text", nullable: true),
                    ContactWorkPhone = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_ApplicationContact", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationContact_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkType = table.Column<string>(type: "text", nullable: false, defaultValue: "Related"),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationLinks_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ApplicationTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationTags_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApplicationTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessorId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalRecommended = table.Column<bool>(type: "boolean", nullable: true),
                    IsAiAssessment = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FinancialAnalysis = table.Column<int>(type: "integer", nullable: true),
                    EconomicImpact = table.Column<int>(type: "integer", nullable: true),
                    InclusiveGrowth = table.Column<int>(type: "integer", nullable: true),
                    CleanGrowth = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_Assessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assessments_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Assessments_Persons_AssessorId",
                        column: x => x.AssessorId,
                        principalTable: "Persons",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScheduledNotificationTracking",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledNotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateField = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NotificationSentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledNotificationTracking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledNotificationTracking_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduledNotificationTracking_ScheduledNotifications_Schedu~",
                        column: x => x.ScheduledNotificationId,
                        principalSchema: "Notifications",
                        principalTable: "ScheduledNotifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Answers",
                schema: "Flex",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentValue = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoresheetInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_Answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Answers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "Flex",
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Answers_ScoresheetInstances_ScoresheetInstanceId",
                        column: x => x.ScoresheetInstanceId,
                        principalSchema: "Flex",
                        principalTable: "ScoresheetInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseApprovals",
                schema: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecisionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
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
                    table.PrimaryKey("PK_ExpenseApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseApprovals_PaymentRequests_PaymentRequestId",
                        column: x => x.PaymentRequestId,
                        principalSchema: "Payments",
                        principalTable: "PaymentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTags",
                schema: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTags_PaymentRequests_PaymentRequestId",
                        column: x => x.PaymentRequestId,
                        principalSchema: "Payments",
                        principalTable: "PaymentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssessmentAttachments",
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
                    FileName = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    S3ObjectKey = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentAttachments_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentComments",
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
                    CommenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PinDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentComments_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentComments_Persons_CommenterId",
                        column: x => x.CommenterId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionId",
                schema: "Flex",
                table: "Answers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_ScoresheetInstanceId",
                schema: "Flex",
                table: "Answers",
                column: "ScoresheetInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantAddresses_ApplicantId",
                table: "ApplicantAddresses",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantAddresses_ApplicationId",
                table: "ApplicantAddresses",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantAgents_ApplicantId",
                table: "ApplicantAgents",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantAgents_ApplicationId",
                table: "ApplicantAgents",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantAgents_TenantId_ApplicationId",
                table: "ApplicantAgents",
                columns: new[] { "TenantId", "ApplicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantAttachments_ApplicantId",
                table: "ApplicantAttachments",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantComments_ApplicantId",
                table: "ApplicantComments",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantComments_CommenterId",
                table: "ApplicantComments",
                column: "CommenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_ApplicantName",
                table: "Applicants",
                column: "ApplicantName");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_OrgName",
                table: "Applicants",
                column: "OrgName");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_OrgNumber",
                table: "Applicants",
                column: "OrgNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_Status",
                table: "Applicants",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_SupplierId",
                table: "Applicants",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_TenantId",
                table: "Applicants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_TenantId_IsDeleted_CreationTime",
                table: "Applicants",
                columns: new[] { "TenantId", "IsDeleted", "CreationTime" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_UnityApplicantId",
                table: "Applicants",
                column: "UnityApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationAssignments_ApplicationId",
                table: "ApplicationAssignments",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationAssignments_AssigneeId",
                table: "ApplicationAssignments",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationAssignments_TenantId_ApplicationId",
                table: "ApplicationAssignments",
                columns: new[] { "TenantId", "ApplicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationAttachments_ApplicationId",
                table: "ApplicationAttachments",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationChefsFileAttachments_ApplicationId",
                table: "ApplicationChefsFileAttachments",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationComments_ApplicationId",
                table: "ApplicationComments",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationComments_CommenterId",
                table: "ApplicationComments",
                column: "CommenterId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationContact_ApplicationId",
                table: "ApplicationContact",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationForms_IntakeId",
                table: "ApplicationForms",
                column: "IntakeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationForms_ParentFormId",
                table: "ApplicationForms",
                column: "ParentFormId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationForms_TenantId_IsDeleted",
                table: "ApplicationForms",
                columns: new[] { "TenantId", "IsDeleted" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationFormSubmissions_ApplicantId",
                table: "ApplicationFormSubmissions",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationFormSubmissions_ApplicationFormId",
                table: "ApplicationFormSubmissions",
                column: "ApplicationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationFormVersion_ApplicationFormId",
                table: "ApplicationFormVersion",
                column: "ApplicationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLinks_ApplicationId",
                table: "ApplicationLinks",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLinks_TenantId_ApplicationId",
                table: "ApplicationLinks",
                columns: new[] { "TenantId", "ApplicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ApplicantId",
                table: "Applications",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ApplicationFormId",
                table: "Applications",
                column: "ApplicationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ApplicationStatusId",
                table: "Applications",
                column: "ApplicationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_OwnerId",
                table: "Applications",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ReferenceNo",
                table: "Applications",
                column: "ReferenceNo");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_TenantId_SubmissionDate",
                table: "Applications",
                columns: new[] { "TenantId", "SubmissionDate" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatuses_StatusCode",
                table: "ApplicationStatuses",
                column: "StatusCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationTags_ApplicationId",
                table: "ApplicationTags",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationTags_TagId",
                table: "ApplicationTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationTags_TenantId_ApplicationId",
                table: "ApplicationTags",
                columns: new[] { "TenantId", "ApplicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttachments_AssessmentId",
                table: "AssessmentAttachments",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentComments_AssessmentId",
                table: "AssessmentComments",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentComments_CommenterId",
                table: "AssessmentComments",
                column: "CommenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_ApplicationId",
                table: "Assessments",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_AssessorId",
                table: "Assessments",
                column: "AssessorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditHistories_ApplicantId",
                table: "AuditHistories",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactLinks_ContactId_RelatedEntityType_RelatedEntityId",
                table: "ContactLinks",
                columns: new[] { "ContactId", "RelatedEntityType", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactLinks_RelatedEntityType_RelatedEntityId",
                table: "ContactLinks",
                columns: new[] { "RelatedEntityType", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomFields_SectionId",
                schema: "Flex",
                table: "CustomFields",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldValues_WorksheetInstanceId",
                schema: "Flex",
                table: "CustomFieldValues",
                column: "WorksheetInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailGroupUsers_GroupId",
                schema: "Notifications",
                table: "EmailGroupUsers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogAttachments_EmailLogId",
                schema: "Notifications",
                table: "EmailLogAttachments",
                column: "EmailLogId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogAttachments_S3ObjectKey",
                schema: "Notifications",
                table: "EmailLogAttachments",
                column: "S3ObjectKey");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogAttachments_TemplateId",
                schema: "Notifications",
                table: "EmailLogAttachments",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseApprovals_PaymentRequestId",
                schema: "Payments",
                table: "ExpenseApprovals",
                column: "PaymentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_FundingHistories_ApplicantId",
                table: "FundingHistories",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueTrackings_ApplicantId",
                table: "IssueTrackings",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_AccountCodingId",
                schema: "Payments",
                table: "PaymentRequests",
                column: "AccountCodingId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_CorrelationId",
                schema: "Payments",
                table: "PaymentRequests",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_CreationTime",
                schema: "Payments",
                table: "PaymentRequests",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_FsbNotificationEmailLogId",
                schema: "Payments",
                table: "PaymentRequests",
                column: "FsbNotificationEmailLogId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_ReferenceNumber",
                schema: "Payments",
                table: "PaymentRequests",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_SiteId",
                schema: "Payments",
                table: "PaymentRequests",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_Status",
                schema: "Payments",
                table: "PaymentRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_TenantId_CreationTime",
                schema: "Payments",
                table: "PaymentRequests",
                columns: new[] { "TenantId", "CreationTime" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTags_PaymentRequestId",
                schema: "Payments",
                table: "PaymentTags",
                column: "PaymentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTags_TagId",
                schema: "Payments",
                table: "PaymentTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_OidcSub",
                table: "Persons",
                column: "OidcSub");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_TenantId",
                table: "Persons",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_SectionId",
                schema: "Flex",
                table: "Questions",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportsHistories_ApplicantId",
                table: "ReportsHistories",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotifications_TenantId",
                schema: "Notifications",
                table: "ScheduledNotifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotificationTracking_ApplicationId",
                schema: "Notifications",
                table: "ScheduledNotificationTracking",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotificationTracking_ApplicationId_ScheduledNotifi~",
                schema: "Notifications",
                table: "ScheduledNotificationTracking",
                columns: new[] { "ApplicationId", "ScheduledNotificationId", "DateField" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotificationTracking_CreationTime",
                schema: "Notifications",
                table: "ScheduledNotificationTracking",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotificationTracking_ScheduledNotificationId",
                schema: "Notifications",
                table: "ScheduledNotificationTracking",
                column: "ScheduledNotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoresheetInstances_ScoresheetId",
                schema: "Flex",
                table: "ScoresheetInstances",
                column: "ScoresheetId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoresheetSections_ScoresheetId",
                schema: "Flex",
                table: "ScoresheetSections",
                column: "ScoresheetId");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_SupplierId",
                schema: "Payments",
                table: "Sites",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionGroupSubscribers_GroupId",
                schema: "Notifications",
                table: "SubscriptionGroupSubscribers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionGroupSubscribers_SubscriberId",
                schema: "Notifications",
                table: "SubscriptionGroupSubscribers",
                column: "SubscriberId");

            migrationBuilder.CreateIndex(
                name: "IX_TriggerSubscriptions_SubscriptionGroupId",
                schema: "Notifications",
                table: "TriggerSubscriptions",
                column: "SubscriptionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TriggerSubscriptions_TemplateId",
                schema: "Notifications",
                table: "TriggerSubscriptions",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TriggerSubscriptions_TriggerId",
                schema: "Notifications",
                table: "TriggerSubscriptions",
                column: "TriggerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorksheetLinks_WorksheetId",
                schema: "Flex",
                table: "WorksheetLinks",
                column: "WorksheetId");

            migrationBuilder.CreateIndex(
                name: "IX_WorksheetSections_WorksheetId",
                schema: "Flex",
                table: "WorksheetSections",
                column: "WorksheetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Answers",
                schema: "Flex");

            migrationBuilder.DropTable(
                name: "ApplicantAddresses");

            migrationBuilder.DropTable(
                name: "ApplicantAgents");

            migrationBuilder.DropTable(
                name: "ApplicantAttachments");

            migrationBuilder.DropTable(
                name: "ApplicantComments");

            migrationBuilder.DropTable(
                name: "ApplicationAssignments");

            migrationBuilder.DropTable(
                name: "ApplicationAttachments");

            migrationBuilder.DropTable(
                name: "ApplicationChefsFileAttachments");

            migrationBuilder.DropTable(
                name: "ApplicationComments");

            migrationBuilder.DropTable(
                name: "ApplicationContact");

            migrationBuilder.DropTable(
                name: "ApplicationFormSubmissions");

            migrationBuilder.DropTable(
                name: "ApplicationFormVersion");

            migrationBuilder.DropTable(
                name: "ApplicationLinks");

            migrationBuilder.DropTable(
                name: "ApplicationTags");

            migrationBuilder.DropTable(
                name: "AssessmentAttachments");

            migrationBuilder.DropTable(
                name: "AssessmentComments");

            migrationBuilder.DropTable(
                name: "AuditHistories");

            migrationBuilder.DropTable(
                name: "ContactLinks");

            migrationBuilder.DropTable(
                name: "CustomFields",
                schema: "Flex");

            migrationBuilder.DropTable(
                name: "CustomFieldValues",
                schema: "Flex");

            migrationBuilder.DropTable(
                name: "EmailGroupUsers",
                schema: "Notifications");

            migrationBuilder.DropTable(
                name: "EmailLogAttachments",
                schema: "Notifications");

            migrationBuilder.DropTable(
                name: "ExpenseApprovals",
                schema: "Payments");

            migrationBuilder.DropTable(
                name: "FundingHistories");

            migrationBuilder.DropTable(
                name: "IssueTrackings");

            migrationBuilder.DropTable(
                name: "PaymentConfigurations",
                schema: "Payments");

            migrationBuilder.DropTable(
                name: "PaymentTags",
                schema: "Payments");

            migrationBuilder.DropTable(
                name: "PaymentThresholds",
                schema: "Payments");

            migrationBuilder.DropTable(
                name: "ReportColumnsMaps",
                schema: "Reporting");

            migrationBuilder.DropTable(
                name: "ReportsHistories");

            migrationBuilder.DropTable(
                name: "ScheduledNotificationTracking",
                schema: "Notifications");

            migrationBuilder.DropTable(
                name: "SubscriptionGroupSubscribers",
                schema: "Notifications");

            migrationBuilder.DropTable(
                name: "TemplateVariables",
                schema: "Notifications");

            migrationBuilder.DropTable(
                name: "TriggerSubscriptions",
                schema: "Notifications");

            migrationBuilder.DropTable(
                name: "WorksheetLinks",
                schema: "Flex");

            migrationBuilder.DropTable(
                name: "Questions",
                schema: "Flex");

            migrationBuilder.DropTable(
                name: "ScoresheetInstances",
                schema: "Flex");

            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "WorksheetSections",
                schema: "Flex");

            migrationBuilder.DropTable(
                name: "WorksheetInstances",
                schema: "Flex");

            migrationBuilder.DropTable(
                name: "EmailGroups",
                schema: "Notifications");

            migrationBuilder.DropTable(
                name: "EmailLogs",
                schema: "Notifications");

            migrationBuilder.DropTable(
                name: "PaymentRequests",
                schema: "Payments");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "ScheduledNotifications",
                schema: "Notifications");

            migrationBuilder.DropTable(
                name: "Subscribers",
                schema: "Notifications");

            migrationBuilder.DropTable(
                name: "EmailTemplates",
                schema: "Notifications");

            migrationBuilder.DropTable(
                name: "SubscriptionGroups",
                schema: "Notifications");

            migrationBuilder.DropTable(
                name: "Triggers",
                schema: "Notifications");

            migrationBuilder.DropTable(
                name: "ScoresheetSections",
                schema: "Flex");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "Worksheets",
                schema: "Flex");

            migrationBuilder.DropTable(
                name: "AccountCodings",
                schema: "Payments");

            migrationBuilder.DropTable(
                name: "Sites",
                schema: "Payments");

            migrationBuilder.DropTable(
                name: "Scoresheets",
                schema: "Flex");

            migrationBuilder.DropTable(
                name: "Applicants");

            migrationBuilder.DropTable(
                name: "ApplicationForms");

            migrationBuilder.DropTable(
                name: "ApplicationStatuses");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "Payments");

            migrationBuilder.DropTable(
                name: "Intakes");
        }
    }
}
