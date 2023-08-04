using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantPrograms;
using Unity.GrantManager.Applications;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using System.Collections.Generic;
using Unity.GrantManager.GrantApplications;

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

        var status4 = await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS04",
                ExternalStatus = "Withdrawn",
                InternalStatus = "Withdrawn"
            }
        );

        var status5 = await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS05",
                ExternalStatus = "Closed",
                InternalStatus = "Closed"
            }
        );

        var status6 = await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS06",
                ExternalStatus = "Under Review",
                InternalStatus = "Under Initial Review"
            }
        );

        var status7 = await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS07",
                ExternalStatus = "Under Review",
                InternalStatus = "Initial Review Completed"
            }
        );

        var status8 = await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS08",
                ExternalStatus = "Under Review",
                InternalStatus = "Under Adjudication"
            }
        );

        var status9 = await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS09",
                ExternalStatus = "Under Review",
                InternalStatus = "Adjudication Completed"
            }
        );

        var status10 = await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "STATUS10",
                ExternalStatus = "Grant Approved",
                InternalStatus = "Grant Approved"
            }
        );

        var status11 = await _applicationStatusRepository.InsertAsync(
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
                ProposalDate = new DateTime(2022, 1, 1),
                SubmissionDate = new DateTime(2023, 1, 1),
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
                ProposalDate = new DateTime(2022, 1, 1),
                SubmissionDate = new DateTime(2023, 1, 1),
                Payload = "{\"Name\":\"John Doe\",\"Age\":45,\"Address\":\"Toronto\"}"
            }, autoSave: true
        );

        var application3 = await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ApplicationFormId = appForm1.Id,
                ApplicationStatusId = status1.Id,
                ProjectName = "New Helicopter Fund",
                ReferenceNo = "ABC123",
                EligibleAmount = 10000.00,
                RequestedAmount = 12500.00,                
                ProposalDate = new DateTime(2022, 10, 02),
                SubmissionDate = new DateTime(2023, 01, 02)                
            }
        );

        var application4 = await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ApplicationFormId = appForm2.Id,
                ApplicationStatusId = status2.Id,
                ProjectName = "Shoebox",
                ReferenceNo = "HCA123",
                EligibleAmount = 22300.00,
                RequestedAmount = 332500.00,                        
                ProposalDate = new DateTime(2022, 11, 03),
                SubmissionDate = new DateTime(2023, 1, 04)                
            }
         );

        var application5 = await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ApplicationFormId = appForm2.Id,
                ApplicationStatusId = status2.Id,
                ProjectName = "Pony Club",
                ReferenceNo = "111BGC",
                EligibleAmount = 2212400.00,
                RequestedAmount = 2312500.00,                
                ProposalDate = new DateTime(2021, 01, 03),
                SubmissionDate = new DateTime(2023, 02, 02)                
            }
        );
        var application6 = await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ApplicationFormId = appForm1.Id,
                ApplicationStatusId = status4.Id,
                ProjectName = "Village Fountain Repair",
                ReferenceNo = "BB11FF",
                EligibleAmount = 13100.00,
                RequestedAmount = 11100.00,
                ProposalDate = new DateTime(2024, 05, 02),
                SubmissionDate = new DateTime(2025, 01, 03)
            }
         );
        var application7 = await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ApplicationFormId = appForm1.Id,
                ApplicationStatusId = status7.Id,
                ProjectName = "Hoover",
                ReferenceNo = "GG1731",
                EligibleAmount = 232400.00,
                RequestedAmount = 332500.00,
                ProposalDate = new DateTime(2022, 10, 02),
                SubmissionDate = new DateTime(2023, 01, 02)
            }
            );
        var application8 = await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ApplicationFormId = appForm2.Id,
                ApplicationStatusId = status5.Id,
                ProjectName = "Tree Planting",
                ReferenceNo = "BBNNGG",
                EligibleAmount = 1312400.00,
                RequestedAmount = 444400.00,
                ProposalDate = new DateTime(2023, 10, 03),
                SubmissionDate = new DateTime(2023, 02, 02)

            }
            );
        var application9 = await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ApplicationFormId = appForm2.Id,
                ApplicationStatusId = status2.Id,
                ProjectName = "Pizza Joint",
                ReferenceNo = "FF13BB",
                EligibleAmount = 332100.00,
                RequestedAmount = 32100.00,
                ProposalDate = new DateTime(2022, 09, 01),
                SubmissionDate = new DateTime(2023, 08, 03)

            }
            );
        var application10 = await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ApplicationFormId = appForm1.Id,
                ApplicationStatusId = status2.Id,
                ProjectName = "Froghopper Express",
                ReferenceNo = "AD1FFB",
                EligibleAmount = 3312300.00,
                RequestedAmount = 11100.00,
                ProposalDate = new DateTime(2022, 11, 03),
                SubmissionDate = new DateTime(2023, 11, 05)                             
            }
            );
        var application11 = await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ApplicationFormId = appForm2.Id,
                ApplicationStatusId = status2.Id,
                ProjectName = "Courtyard Landscaping",
                ReferenceNo = "AF17GB",
                EligibleAmount = 12400.00,
                RequestedAmount = 22500.00,
                ProposalDate = new DateTime(2022, 10, 02),
                SubmissionDate = new DateTime(2023, 01, 02),
            }
            );
        var application12 = await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ApplicationFormId = appForm2.Id,
                ApplicationStatusId = status2.Id,
                ProjectName = "Disco Ball",
                ReferenceNo = "AF11BB",
                EligibleAmount = 1400.00,
                RequestedAmount = 3500.00,
                ProposalDate = new DateTime(2023, 10, 03),
                SubmissionDate = new DateTime(2023, 11, 02)
            }
            );
        var application13 = await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ApplicationFormId = appForm1.Id,
                ApplicationStatusId = status2.Id,
                ProjectName = "Gymnasium Repair",
                ReferenceNo = "GYM007",
                EligibleAmount = 332400.00,
                RequestedAmount = 112500.00,
                ProposalDate = new DateTime(2023, 10, 02),
                SubmissionDate = new DateTime(2023, 01, 02)
            }
            );
        var application14 = await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ApplicationFormId = appForm1.Id,
                ApplicationStatusId = status4.Id,
                ProjectName = "Holiday Abroad Funding",
                ReferenceNo = "BG22CD",
                EligibleAmount = 23400.00,
                RequestedAmount = 33500.00,
                ProposalDate = new DateTime(2022, 10, 02),
                SubmissionDate = new DateTime(2023, 01, 02)
            }
            );
    }
}
