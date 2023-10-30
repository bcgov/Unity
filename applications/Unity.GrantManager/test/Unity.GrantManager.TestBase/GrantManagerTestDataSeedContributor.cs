using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Comments;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantPrograms;
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
    private readonly IIdentityUserRepository _userRepository;

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
        IIdentityUserRepository userRepository)
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
        _userRepository = userRepository;
        _assessmentAttachmentRepository = assessmentAttachmentRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        await CreateUsersAsync();
        await CreateSeedDataAsync();
    }

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
                RequestedAmount = 3456.13,
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
                Comment = "Test Comment"
            },
            autoSave: true
        );

        ApplicationAttachment? applicationAttachment1 = await _applicationAttachmentRepository.FindAsync(GrantManagerTestData.ApplicationAttachment1_Id);
        applicationAttachment1 ??= await _applicationAttachmentRepository.InsertAsync(
            new ApplicationAttachmentSeed(GrantManagerTestData.ApplicationAttachment1_Id)
            {
                ApplicationId = GrantManagerTestData.Application1_Id,
                S3ObjectKey = "Unity/Development/Application/report.pdf",
                UserId = "00000000-0000-0000-0000-000000000000",
                FileName = "report.pdf",
                AttachedBy = "John Doe",
                Time = DateTime.Now,
            },
            autoSave: true
        );

        Assessment? assessment1 = await _assessmentRepository.FindAsync(GrantManagerTestData.Assessment1_Id);
        assessment1 ??= await _assessmentRepository.InsertAsync(
            new Assessment
            (
                id: GrantManagerTestData.Assessment1_Id,
                applicationId: GrantManagerTestData.Application1_Id,
                assessorId: GrantManagerTestData.User_Assessor1_UserId,
                AssessmentState.IN_PROGRESS
            ));

        AssessmentAttachment? assessmentAttachment1 = await _assessmentAttachmentRepository.FindAsync(GrantManagerTestData.AssessmentAttachment1_Id);
        assessmentAttachment1 ??= await _assessmentAttachmentRepository.InsertAsync(
            new AssessmentAttachmentSeed(GrantManagerTestData.AssessmentAttachment1_Id)
            {
                AssessmentId = GrantManagerTestData.Assessment1_Id,
                S3ObjectKey = "Unity/Development/Assessment/result.pdf",
                UserId = GrantManagerTestData.User_Assessor1_UserId,
                FileName = "result.pdf",
                AttachedBy = "John Doe",
                Time = DateTime.UtcNow
            },
            autoSave: true
        );

        AssessmentComment? assessmentComment1 = await _assessmentCommentRepository.FindAsync(GrantManagerTestData.AssessmentComment1_Id);
        assessmentComment1 ??= await _assessmentCommentRepository.InsertAsync(
            new AssessmentCommentSeed(GrantManagerTestData.AssessmentComment1_Id)
            {
                AssessmentId = GrantManagerTestData.Assessment1_Id,
                Comment = "Test Comment"
            },
            autoSave: true
        );
    }

    private async Task CreateUsersAsync()
    {
        var user1 = await _userRepository.FindAsync(GrantManagerTestData.User_Assessor1_UserId);
        if (user1 == null)
        {
            await _userRepository.InsertAsync(
            new IdentityUser(
                GrantManagerTestData.User_Assessor1_UserId,
                GrantManagerTestData.User_Assessor1_UserName,
                GrantManagerTestData.User_Assessor1_EmailAddress));
        }

        var user2 = await _userRepository.FindAsync(GrantManagerTestData.User_Assessor2_UserId);
        if (user2 == null)
        {
            await _userRepository.InsertAsync(
            new IdentityUser(
                GrantManagerTestData.User_Assessor2_UserId,
                GrantManagerTestData.User_Assessor2_UserName,
                GrantManagerTestData.User_Assessor2_EmailAddress));
        }
    }
}