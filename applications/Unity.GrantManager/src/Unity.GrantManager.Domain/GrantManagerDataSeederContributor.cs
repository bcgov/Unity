using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantPrograms;
using Unity.GrantManager.Applications;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager;

public class GrantManagerDataSeederContributor
    : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<GrantProgram, Guid> _grantProgramRepository;
    private readonly IIntakeRepository _intakeRepository;
    private readonly IApplicationFormRepository _applicationFormRepository;
    private readonly IApplicantRepository _applicantRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationStatusRepository _applicationStatusRepository;

    public GrantManagerDataSeederContributor(IRepository<GrantProgram, Guid> grantProgramRepository,
        IIntakeRepository intakeRepository,
        IApplicationFormRepository applicationFormRepository,
        IApplicantRepository applicantRepository,
        IApplicationRepository applicationRepository,
        IApplicationStatusRepository applicationStatusRepository)
    {
        _grantProgramRepository = grantProgramRepository;
        _intakeRepository = intakeRepository;
        _applicationFormRepository = applicationFormRepository;
        _applicantRepository = applicantRepository;
        _applicationRepository = applicationRepository;
        _applicationStatusRepository = applicationStatusRepository;
    }


    public async Task SeedAsync(DataSeedContext context)
    {
        var spaceFarms = await _grantProgramRepository.InsertAsync(
            new GrantProgram
            {
                ProgramName = "Space Farms Grant Program",
                Type = GrantProgramType.Agriculture,
                PublishDate = new DateTime(2023, 6, 8),
            },
            autoSave: true
        );

        var fictionalArts = await _grantProgramRepository.InsertAsync(
            new GrantProgram
            {
                ProgramName = "Fictional Arts Accelerator Grant",
                Type = GrantProgramType.Arts,
                PublishDate = new DateTime(2023, 5, 15),
            },
            autoSave: true
        );

        var newApproaches = await _grantProgramRepository.InsertAsync(
            new GrantProgram
            {
                ProgramName = "New Approaches in Counting Grant",
                Type = GrantProgramType.Research,
                PublishDate = new DateTime(2020, 5, 15),
            },
            autoSave: true
        );

        var bizBusiness = await _grantProgramRepository.InsertAsync(
            new GrantProgram
            {
                ProgramName = "BizBusiness Fund",
                Type = GrantProgramType.Business,
                PublishDate = new DateTime(1992, 01, 01),
            },
            autoSave: true
        );

        var historicalBooks = await _grantProgramRepository.InsertAsync(
            new GrantProgram
            {
                ProgramName = "Historically Small Books Preservation Grant",
                Type = GrantProgramType.Arts,
                PublishDate = new DateTime(2002, 01, 01),
            },
            autoSave: true
        );

        var spaceFarmsIntake1 = await _intakeRepository.InsertAsync(
            new Intake
            {
                IntakeName = "2022 Intake",
                StartDate = new DateOnly(2022,1,1),
                EndDate = new DateOnly(2023,1,1),
            },
            autoSave: true
        );

        var spaceFarmsIntake2 = await _intakeRepository.InsertAsync(
            new Intake
            {
                IntakeName = "2023 Intake",
                StartDate = new DateOnly(2023,1,1),
                EndDate = new DateOnly(2024,1,1),
            },
            autoSave: true
        );

        var appForm1 = await _applicationFormRepository.InsertAsync(
            new ApplicationForm
            {
                IntakeId = spaceFarmsIntake1.Id,
                ApplicationFormName = "Space Farms Intake 1 Form 1",
                ChefsApplicationFormGuid="123456",
                ChefsCriteriaFormGuid="213121"
            },
            autoSave: true
        );

        var appForm2 = await _applicationFormRepository.InsertAsync(
            new ApplicationForm
            {
                IntakeId = spaceFarmsIntake1.Id,
                ApplicationFormName = "Space Farms Intake 1 Form 2",
                ChefsApplicationFormGuid = "123456",
                ChefsCriteriaFormGuid = "213121"
            },
            autoSave: true
        );

        var applicant1 = await _applicantRepository.InsertAsync(
            new Applicant { 
                ApplicantName = " John Smith" 
            }, autoSave: true
        );        

        var status1 = await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS01",
                ExternalStatus = "In progress",
                InternalStatus = "In progress"
            }
        );

        var status2 = await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS02",
                ExternalStatus = "Submitted",
                InternalStatus = "Submitted"
            }
        );

        var status3 = await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS03",
                ExternalStatus = "Under Review",
                InternalStatus = "Assigned"
            }
        );

        await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS04",
                ExternalStatus = "Withdrawn",
                InternalStatus = "Withdrawn"
            }
        );

        await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS05",
                ExternalStatus = "Closed",
                InternalStatus = "Closed"
            }
        );

        await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS06",
                ExternalStatus = "Under Review",
                InternalStatus = "Under Initial Review"
            }
        );

        await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS07",
                ExternalStatus = "Under Review",
                InternalStatus = "Initial Review Completed"
            }
        );

        await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS08",
                ExternalStatus = "Under Review",
                InternalStatus = "Under Adjudication"
            }
        );

        await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS09",
                ExternalStatus = "Under Review",
                InternalStatus = "Adjudication Completed"
            }
        );

        await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS10",
                ExternalStatus = "Grant Approved",
                InternalStatus = "Grant Approved"
            }
        );

        await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS11",
                ExternalStatus = "Grant Not Approved",
                InternalStatus = "Grant Not Approved"
            }
        );

        var application1 = await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ProjectName = "Application For Space Farms Grant",
                ApplicationFormId = appForm1.Id,
                ApplicationStatusId = status1.Id,
                ReferenceNo = "1234",
                EligibleAmount = 123.4,
                RequestedAmount = 231.4,
                ProposalDate = new DateOnly(2022, 1, 1),
                SubmissionDate = new DateOnly(2023, 1, 1),
                Payload = "{\"Name\":\"John Smith\",\"Age\":34,\"Address\":\"British Columbia\"}"
            }, autoSave: true
        );

        var application2 = await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ProjectName = "Application For BizBusiness Fund",
                ApplicationFormId = appForm2.Id,
                ApplicationStatusId = status2.Id,
                ReferenceNo =  "3445",
                EligibleAmount = 345.5,
                RequestedAmount = 765.4,
                ProposalDate = new DateOnly(2022, 1, 1),
                SubmissionDate = new DateOnly(2023, 1, 1),
                Payload = "{\"Name\":\"John Doe\",\"Age\":45,\"Address\":\"Toronto\"}"
            }, autoSave: true
        );
    }
}
