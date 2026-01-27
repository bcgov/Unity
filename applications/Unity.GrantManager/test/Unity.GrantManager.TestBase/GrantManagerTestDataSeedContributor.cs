using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Comments;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Intakes;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Unity.GrantManager;

public class GrantManagerTestDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<Application, Guid> _applicationRepository;
    private readonly IRepository<ApplicationStatus, Guid> _applicationStatusRepository;
    private readonly IRepository<Applicant, Guid> _applicantRepository;
    private readonly IRepository<ApplicationForm, Guid> _applicationFormRepository;
    private readonly IRepository<Intake, Guid> _intakeRepository;
    private readonly IRepository<Assessment, Guid> _assessmentRepository;
    private readonly IRepository<AssessmentComment, Guid> _assessmentCommentRepository;
    private readonly IRepository<ApplicationComment, Guid> _applicationCommentRepository;
    private readonly IApplicationAttachmentRepository _applicationAttachmentRepository;
    private readonly IAssessmentAttachmentRepository _assessmentAttachmentRepository;
    private readonly IApplicationChefsFileAttachmentRepository _applicationChefsFileAttachmentRepository;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IPersonRepository _personRepository;

#pragma warning disable S107 // Methods should not have too many parameters
    public GrantManagerTestDataSeedContributor(
        IRepository<Application, Guid> applicationRepository,
        IRepository<ApplicationStatus, Guid> applicationStatusRepository,
        IRepository<Applicant, Guid> applicantRepository,
        IRepository<ApplicationForm, Guid> applicationFormRepository,
        IRepository<Intake, Guid> intakeRepository,
        IRepository<Assessment, Guid> assessmentRepository,
        IRepository<AssessmentComment, Guid> assessmentCommentRepository,
        IRepository<ApplicationComment, Guid> applicationCommentRepository,
        IApplicationAttachmentRepository applicationAttachmentRepository,
        IAssessmentAttachmentRepository assessmentAttachmentRepository,
        IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
        IIdentityUserRepository userRepository,
        IPersonRepository personRepository)
#pragma warning restore S107 // Methods should not have too many parameters
    {
        _applicationRepository = applicationRepository;
        _applicationStatusRepository = applicationStatusRepository;
        _applicantRepository = applicantRepository;
        _applicationFormRepository = applicationFormRepository;
        _intakeRepository = intakeRepository;
        _assessmentRepository = assessmentRepository;
        _assessmentCommentRepository = assessmentCommentRepository;
        _applicationCommentRepository = applicationCommentRepository;
        _applicationAttachmentRepository = applicationAttachmentRepository;
        _assessmentAttachmentRepository = assessmentAttachmentRepository;
        _applicationChefsFileAttachmentRepository = applicationChefsFileAttachmentRepository;
        _userRepository = userRepository;
        _personRepository = personRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        await CreateUsersAsync();
        await CreateSeedDataAsync();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1854:Unused assignments should be removed", Justification = "Data Seed Contributor")]
    private async Task CreateSeedDataAsync()
    {
        Applicant? applicant1 = await _applicantRepository.FindAsync(GrantManagerTestData.Applicant1_Id);
        applicant1 ??= await _applicantRepository.InsertAsync(
            new ApplicantSeed(GrantManagerTestData.Applicant1_Id)
            {
                ApplicantName = "Integration Tester 1"
            },
            autoSave: true
        );

        Intake? spaceFarmsIntake1 = await _intakeRepository.FindAsync(GrantManagerTestData.Intake1_Id);
        spaceFarmsIntake1 ??= await _intakeRepository.InsertAsync(
                new IntakeSeed(GrantManagerTestData.Intake1_Id)
                {
                    IntakeName = "Integration Tests Intake",
                    StartDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
            autoSave: true
        );


        ApplicationForm? appForm1 = await _applicationFormRepository.FindAsync(GrantManagerTestData.ApplicationForm1_Id);
        appForm1 ??= await _applicationFormRepository.InsertAsync(
            new ApplicationFormSeed(GrantManagerTestData.ApplicationForm1_Id)
            {
                IntakeId = GrantManagerTestData.Intake1_Id,
                ApplicationFormName = "Integration Tests Form 1",
                ChefsApplicationFormGuid = "00000000-0000-0000-0000-000000000000",
                ApiKey = "AAAAAAAAAAAAAAAAAAAA",
                ChefsCriteriaFormGuid = "00000000-0000-0000-0000-000000000000"
            },
            autoSave: true
        );

        // We add this here explicilty to add it to the sql lite db
        ApplicationStatus? status1 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == GrantApplicationState.SUBMITTED);
        status1 ??= await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = GrantApplicationState.SUBMITTED,
                ExternalStatus = "Submitted",
                InternalStatus = "Submitted"
            },
            autoSave: true
        );

        Application? application1 = await _applicationRepository.FindAsync(GrantManagerTestData.Application1_Id);
        application1 ??= await _applicationRepository.InsertAsync(
            new ApplicationSeed(GrantManagerTestData.Application1_Id)
            {
                ApplicantId = GrantManagerTestData.Applicant1_Id,
                ProjectName = "Application For Integration Test Funding",
                ApplicationFormId = GrantManagerTestData.ApplicationForm1_Id,
                ApplicationStatusId = (await _applicationStatusRepository.GetAsync(x => x.StatusCode == GrantApplicationState.SUBMITTED)).Id,
                ReferenceNo = "TEST12345",
                ApprovedAmount = 12345.51m,
                RequestedAmount = 3456.13m,
                ProposalDate = new DateTime(2022, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc),
                SubmissionDate = new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Utc),
                Payload = "{\"Name\":\"John Smith\",\"Age\":34,\"Address\":\"British Columbia\"}"
            },
            autoSave: true
        );

        ApplicationComment? applicationComment1 = await _applicationCommentRepository.FindAsync(GrantManagerTestData.ApplicationComment1_Id);
        applicationComment1 ??= await _applicationCommentRepository.InsertAsync(
            new ApplicationCommentSeed(GrantManagerTestData.ApplicationComment1_Id)
            {
                ApplicationId = application1.Id,
                Comment = "Test Comment",
                CommenterId = GrantManagerTestData.User2_UserId
            },
            autoSave: true
        );

        ApplicationAttachment? applicationAttachment1 = await _applicationAttachmentRepository.FindAsync(GrantManagerTestData.ApplicationAttachment1_Id);
        applicationAttachment1 ??= await _applicationAttachmentRepository.InsertAsync(
            new ApplicationAttachmentSeed(GrantManagerTestData.ApplicationAttachment1_Id)
            {
                ApplicationId = GrantManagerTestData.Application1_Id,
                S3ObjectKey = "Unity/Development/Application/report.pdf",
                UserId = GrantManagerTestData.User2_UserId,
                FileName = "report.pdf",                
                Time = DateTime.UtcNow,
            },
            autoSave: true
        );

        Assessment? assessment1 = await _assessmentRepository.FindAsync(GrantManagerTestData.Assessment1_Id);
        assessment1 ??= await _assessmentRepository.InsertAsync(
            new Assessment
            (
                id: GrantManagerTestData.Assessment1_Id,
                applicationId: GrantManagerTestData.Application1_Id,
                assessorId: GrantManagerTestData.User1_UserId,
                AssessmentState.IN_PROGRESS
            ), autoSave: true);

        AssessmentAttachment? assessmentAttachment1 = await _assessmentAttachmentRepository.FindAsync(GrantManagerTestData.AssessmentAttachment1_Id);
        assessmentAttachment1 ??= await _assessmentAttachmentRepository.InsertAsync(
            new AssessmentAttachmentSeed(GrantManagerTestData.AssessmentAttachment1_Id)
            {
                AssessmentId = GrantManagerTestData.Assessment1_Id,
                S3ObjectKey = "Unity/Development/Assessment/result.pdf",
                UserId = GrantManagerTestData.User1_UserId,
                FileName = "result.pdf",                
                Time = DateTime.UtcNow
            },
            autoSave: true
        );

        ApplicationChefsFileAttachment? applicationChefsFileAttachment1 = await _applicationChefsFileAttachmentRepository.FindAsync(GrantManagerTestData.ApplicationAttachment1_Id);
        applicationChefsFileAttachment1 ??= await _applicationChefsFileAttachmentRepository.InsertAsync(
            new ApplicationChefsFileAttachment
            {
                ApplicationId = GrantManagerTestData.Application1_Id,
                FileName = "test.pdf",
                ChefsSubmissionId = "00000000-0000-0000-0000-000000000000",
                ChefsFileId = "00000000-0000-0000-0000-000000000000",
            },
            autoSave: true
        );

        AssessmentComment? assessmentComment1 = await _assessmentCommentRepository.FindAsync(GrantManagerTestData.AssessmentComment1_Id);
        assessmentComment1 ??= await _assessmentCommentRepository.InsertAsync(
            new AssessmentCommentSeed(GrantManagerTestData.AssessmentComment1_Id)
            {
                AssessmentId = GrantManagerTestData.Assessment1_Id,
                Comment = "Test Comment",
                CommenterId = GrantManagerTestData.User1_UserId
            },
            autoSave: true
        );
    }

    private async Task CreateUsersAsync()
    {
        var user1 = await _userRepository.FindAsync(GrantManagerTestData.User1_UserId);
        if (user1 == null)
        {
            await _userRepository.InsertAsync(
            new IdentityUser(
                GrantManagerTestData.User1_UserId,
                GrantManagerTestData.User1_UserName,
                GrantManagerTestData.User1_EmailAddress), autoSave: true);

            await _personRepository.InsertAsync(new Person()
            {
                Id = GrantManagerTestData.User1_UserId,
                Badge = "UT",
                FullName = "Test User 1",
                OidcDisplayName = "Test User 1 : Test",
                OidcSub = "TestUser1"
            }, autoSave: true);
        }

        var user2 = await _userRepository.FindAsync(GrantManagerTestData.User2_UserId);
        if (user2 == null)
        {
            await _userRepository.InsertAsync(
            new IdentityUser(
                GrantManagerTestData.User2_UserId,
                GrantManagerTestData.User2_UserName,
                GrantManagerTestData.User2_EmailAddress), autoSave: true);


            await _personRepository.InsertAsync(new Person()
            {
                Id = GrantManagerTestData.User2_UserId,
                Badge = "UT",
                FullName = "Test User 2",
                OidcDisplayName = "Test User 2 : Test",
                OidcSub = "TestUser2"
            }, autoSave: true);
        }
    }
}