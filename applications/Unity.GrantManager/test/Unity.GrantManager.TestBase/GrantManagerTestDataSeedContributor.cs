using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Comments;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantPrograms;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

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


    public GrantManagerTestDataSeedContributor(IRepository<Application, Guid> applicationRepository,
        IRepository<ApplicationStatus, Guid> applicationStatusRepository,
        IRepository<Applicant, Guid> applicantRepository,
        IRepository<ApplicationForm, Guid> applicationFormRepository,
        IRepository<Intake, Guid> intakeRepository,
        IRepository<Assessment, Guid> assessmentRepository,
        IRepository<AssessmentComment, Guid> assessmentCommentRepository,
        IRepository<ApplicationComment, Guid> applicationCommentRepository)
    {
        _applicationRepository = applicationRepository;
        _applicationStatusRepository = applicationStatusRepository;
        _applicantRepository = applicantRepository;
        _applicationFormRepository = applicationFormRepository;
        _intakeRepository = intakeRepository;
        _assessmentRepository = assessmentRepository;
        _assessmentCommentRepository = assessmentCommentRepository;
        _applicationCommentRepository = applicationCommentRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        /* Data seeded in the app is also seeded in the tests */
        /* Seed additional test data... */

        Applicant? applicant1 = await _applicantRepository.FirstOrDefaultAsync(s => s.ApplicantName == "Integration Tester 1");
        applicant1 ??= await _applicantRepository.InsertAsync(
            new Applicant
            {
                ApplicantName = "Integration Tester 1"
            },
            autoSave: true
        );

        ApplicationStatus? appStatus1 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == ApplicationStatusConsts.SUBMITTED);
        appStatus1 ??= await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "SUBMITTED",
                ExternalStatus = "Submitted",
                InternalStatus = "Submitted"
            },
            autoSave: true
        );

        Intake? spaceFarmsIntake1 = await _intakeRepository.FirstOrDefaultAsync(s => s.IntakeName == "Integration Tests Intake");
        spaceFarmsIntake1 ??= await _intakeRepository.InsertAsync(
                new Intake
                {
                    IntakeName = "Integration Tests Intake",
                    StartDate = new DateOnly(2022, 1, 1),
                    EndDate = new DateOnly(2023, 1, 1),
                },
            autoSave: true
        );


        ApplicationForm? appForm1 = await _applicationFormRepository.FirstOrDefaultAsync(s => s.ApplicationFormName == "Integration Tests Form 1");
        appForm1 ??= await _applicationFormRepository.InsertAsync(
            new ApplicationForm
            {
                IntakeId = spaceFarmsIntake1.Id,
                ApplicationFormName = "Integration Tests Form 1",
                ChefsApplicationFormGuid = "123456",
                ChefsCriteriaFormGuid = "213121"
            },
            autoSave: true
        );

        Application? application1 = (await _applicationRepository.GetQueryableAsync()).FirstOrDefault(s => s.ProjectName == "Integration Tests 1");
        application1 ??= await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ProjectName = "Application For Integration Test Funding",                
                ApplicationFormId = appForm1.Id,
                ApplicationStatusId = appStatus1.Id,
                ReferenceNo = "TEST12345",
                EligibleAmount = 12345.51,
                RequestedAmount = 3456.13,
                ProposalDate =  new DateTime(2022, 1, 1, 12, 0, 0, 0, DateTimeKind.Local),
                SubmissionDate = new DateTime(2023, 1, 1, 12, 0, 0, 0, DateTimeKind.Local),
                Payload = "{\"Name\":\"John Smith\",\"Age\":34,\"Address\":\"British Columbia\"}"
            },
            autoSave: true
        );

        ApplicationComment applicationComment1 = await _applicationCommentRepository.FirstOrDefaultAsync(s => s.ApplicationId == application1.Id);
        applicationComment1 ??= await _applicationCommentRepository.InsertAsync(
            new ApplicationComment
            {
                ApplicationId = application1.Id,
                Comment = "Test Comment"
            },
            autoSave: true
        );

        Assessment assessment1 = await _assessmentRepository.FirstOrDefaultAsync(s => s.ApplicationId == application1.Id);
        assessment1 ??= await _assessmentRepository.InsertAsync(
            new Assessment
            {
                ApplicationId = application1.Id                
            },
            autoSave: true
        );

        AssessmentComment assessmentComment1 = await _assessmentCommentRepository.FirstOrDefaultAsync(s => s.AssessmentId == assessment1.Id);
        assessmentComment1 ??= await _assessmentCommentRepository.InsertAsync(
            new AssessmentComment
            {
                AssessmentId = assessment1.Id,
                Comment = "Test Comment"
            },
            autoSave: true
        );
    }
}
