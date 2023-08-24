using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantPrograms;
using Unity.GrantManager.Applications;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace Unity.GrantManager;

public class GrantManagerDataSeederContributor
    : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<GrantProgram, Guid> _grantProgramRepository;
    private readonly IRepository<Intake, Guid> _intakeRepository;
    private readonly IRepository<ApplicationForm, Guid> _applicationFormRepository;
    private readonly IRepository<Applicant, Guid> _applicantRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationStatusRepository _applicationStatusRepository;
    private readonly IApplicationUserAssignmentRepository _applicationUserAssignmentRepository;
    private readonly IdentityUserManager _identityUserManager;

    public GrantManagerDataSeederContributor(IRepository<GrantProgram, Guid> grantProgramRepository,
        IRepository<Intake, Guid> intakeRepository,
        IRepository<ApplicationForm, Guid> applicationFormRepository,
        IRepository<Applicant, Guid> applicantRepository,
        IApplicationRepository applicationRepository,
        IApplicationStatusRepository applicationStatusRepository,
        IApplicationUserAssignmentRepository applicationUserAssignmentRepository,
        IdentityUserManager identityUserManager)
    {
        _grantProgramRepository = grantProgramRepository;
        _intakeRepository = intakeRepository;
        _applicationFormRepository = applicationFormRepository;
        _applicantRepository = applicantRepository;
        _applicationRepository = applicationRepository;
        _applicationStatusRepository = applicationStatusRepository;
        _applicationUserAssignmentRepository = applicationUserAssignmentRepository;
        _identityUserManager = identityUserManager;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        // TODO: Neaten up with Upsert

        if (!await _grantProgramRepository.AnyAsync(s => s.ProgramName == "Space Farms Grant Program"))
        {
            await _grantProgramRepository.InsertAsync(
                new GrantProgram
                {
                    ProgramName = "Space Farms Grant Program",
                    Type = GrantProgramType.Agriculture,
                    PublishDate = new DateTime(2023, 6, 8),
                },
                autoSave: true
            );
        }

        if (!await _grantProgramRepository.AnyAsync(s => s.ProgramName == "Fictional Arts Accelerator Grant"))
        {
            await _grantProgramRepository.InsertAsync(
                 new GrantProgram
                 {
                     ProgramName = "Fictional Arts Accelerator Grant",
                     Type = GrantProgramType.Arts,
                     PublishDate = new DateTime(2023, 5, 15),
                 },
                 autoSave: true
             );
        }

        if (!await _grantProgramRepository.AnyAsync(s => s.ProgramName == "New Approaches in Counting Grant"))
        {
            await _grantProgramRepository.InsertAsync(
                new GrantProgram
                {
                    ProgramName = "New Approaches in Counting Grant",
                    Type = GrantProgramType.Research,
                    PublishDate = new DateTime(2020, 5, 15),
                },
                autoSave: true
            );
        }

        if (!await _grantProgramRepository.AnyAsync(s => s.ProgramName == "BizBusiness Fund"))
        {
            await _grantProgramRepository.InsertAsync(
                new GrantProgram
                {
                    ProgramName = "BizBusiness Fund",
                    Type = GrantProgramType.Business,
                    PublishDate = new DateTime(1992, 01, 01),
                },
                autoSave: true
            );
        }

        if (!await _grantProgramRepository.AnyAsync(s => s.ProgramName == "Historically Small Books Preservation Grant"))
        {
            await _grantProgramRepository.InsertAsync(
                new GrantProgram
                {
                    ProgramName = "Historically Small Books Preservation Grant",
                    Type = GrantProgramType.Arts,
                    PublishDate = new DateTime(2002, 01, 01),
                },
                autoSave: true
            );
        }

        Intake? spaceFarmsIntake1 = await _intakeRepository.FirstOrDefaultAsync(s => s.IntakeName == "2022 Intake");
        spaceFarmsIntake1 ??= await _intakeRepository.InsertAsync(
                new Intake
                {
                    IntakeName = "2022 Intake",
                    StartDate = new DateOnly(2022, 1, 1),
                    EndDate = new DateOnly(2023, 1, 1),
                },
                autoSave: true
        );

        Intake? spaceFarmsIntake2 = await _intakeRepository.FirstOrDefaultAsync(s => s.IntakeName == "2023 Intake");
        spaceFarmsIntake2 ??= await _intakeRepository.InsertAsync(
            new Intake
            {
                IntakeName = "2023 Intake",
                StartDate = new DateOnly(2023, 1, 1),
                EndDate = new DateOnly(2024, 1, 1),
            },
            autoSave: true
        );

        ApplicationForm? appForm1 = await _applicationFormRepository.FirstOrDefaultAsync(s => s.ApplicationFormName == "Space Farms Intake 1 Form 1");
        appForm1 ??= await _applicationFormRepository.InsertAsync(
            new ApplicationForm
            {
                IntakeId = spaceFarmsIntake1.Id,
                ApplicationFormName = "Space Farms Intake 1 Form 1",
                ChefsApplicationFormGuid = "123456",
                ChefsCriteriaFormGuid = "213121"
            },
            autoSave: true
        );

        ApplicationForm? appForm2 = await _applicationFormRepository.FirstOrDefaultAsync(s => s.ApplicationFormName == "Space Farms Intake 1 Form 2");
        appForm2 ??= await _applicationFormRepository.InsertAsync(
            new ApplicationForm
            {
                IntakeId = spaceFarmsIntake1.Id,
                ApplicationFormName = "Space Farms Intake 1 Form 2",
                ChefsApplicationFormGuid = "123456",
                ChefsCriteriaFormGuid = "213121"
            },
            autoSave: true
        );

        Applicant? applicant1 = await _applicantRepository.FirstOrDefaultAsync(s => s.ApplicantName == " John Smith");
        applicant1 ??= await _applicantRepository.InsertAsync(
            new Applicant
            {
                ApplicantName = " John Smith"
            }, autoSave: true
        );

        ApplicationStatus? status1 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == ApplicationStatusConsts.IN_PROGRESS);
        status1 ??= await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = ApplicationStatusConsts.IN_PROGRESS,
                ExternalStatus = "In progress",
                InternalStatus = "In progress"
            }
        );

        ApplicationStatus? status2 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == ApplicationStatusConsts.SUBMITTED);
        status2 ??= await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "SUBMITTED",
                ExternalStatus = "Submitted",
                InternalStatus = "Submitted"
            }
        );

        ApplicationStatus? status3 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == ApplicationStatusConsts.ASSIGNED);
        status3 ??= await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "ASSIGNED",
                ExternalStatus = "Under Review",
                InternalStatus = "Assigned"
            }
        );

        ApplicationStatus? status4 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == ApplicationStatusConsts.WITHDRAWN);
        status4 ??= await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "WITHDRAWN",
                ExternalStatus = "Withdrawn",
                InternalStatus = "Withdrawn"
            }
        );

        ApplicationStatus? status5 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == ApplicationStatusConsts.CLOSED);
        status5 ??= await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "CLOSED",
                ExternalStatus = "Closed",
                InternalStatus = "Closed"
            }
        );

        ApplicationStatus? status6 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == ApplicationStatusConsts.UNDER_INITIAL_REVIEW);
        status6 ??= await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "UNDER_INITIAL_REVIEW",
                ExternalStatus = "Under Review",
                InternalStatus = "Under Initial Review"
            }
        );

        ApplicationStatus? status7 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == ApplicationStatusConsts.INITITAL_REVIEW_COMPLETED);
        status7 ??= await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "INITITAL_REVIEW_COMPLETED",
                ExternalStatus = "Under Review",
                InternalStatus = "Initial Review Completed"
            }
        );

        ApplicationStatus? status8 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == ApplicationStatusConsts.UNDER_ADJUDICATION);
        status8 ??= await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "UNDER_ADJUDICATION",
                ExternalStatus = "Under Review",
                InternalStatus = "Under Adjudication"
            }
        );

        ApplicationStatus? status9 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == ApplicationStatusConsts.ADJUDICATION_COMPLETED);
        status9 ??= await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "ADJUDICATION_COMPLETED",
                ExternalStatus = "Under Review",
                InternalStatus = "Adjudication Completed"
            }
        );

        ApplicationStatus? status10 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == ApplicationStatusConsts.GRANT_APPROVED);
        status10 ??= await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "GRANT_APPROVED",
                ExternalStatus = "Grant Approved",
                InternalStatus = "Grant Approved"
            }
        );

        ApplicationStatus? status11 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == ApplicationStatusConsts.GRANT_NOT_APPROVED);
        status11 ??= await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "GRANT_NOT_APPROVED",
                ExternalStatus = "Grant Not Approved",
                InternalStatus = "Grant Not Approved"
            }
        );

        Application? application1 = await _applicationRepository.FirstOrDefaultAsync(s => s.ProjectName == "Application For Space Farms Grant");
        application1 ??= await _applicationRepository.InsertAsync(
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

        Application? application2 = await _applicationRepository.FirstOrDefaultAsync(s => s.ProjectName == "Application For BizBusiness Fund");
        application2 ??= await _applicationRepository.InsertAsync(
            new Application
            {
                ApplicantId = applicant1.Id,
                ProjectName = "Application For BizBusiness Fund",
                ApplicationFormId = appForm2.Id,
                ApplicationStatusId = status2.Id,
                ReferenceNo = "3445",
                EligibleAmount = 345.5,
                RequestedAmount = 765.4,
                ProposalDate = new DateTime(2022, 1, 1),
                SubmissionDate = new DateTime(2023, 1, 1),
                Payload = "{\"Name\":\"John Doe\",\"Age\":45,\"Address\":\"Toronto\"}"
            }, autoSave: true
        );

        Application? application3 = await _applicationRepository.FirstOrDefaultAsync(s => s.ProjectName == "New Helicopter Fund");
        application3 ??= await _applicationRepository.InsertAsync(
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
            });

        Application? application4 = await _applicationRepository.FirstOrDefaultAsync(s => s.ProjectName == "Shoebox");
        application4 ??= await _applicationRepository.InsertAsync(
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
            });

        Application? application5 = await _applicationRepository.FirstOrDefaultAsync(s => s.ProjectName == "Pony Club");
        application5 ??= await _applicationRepository.InsertAsync(
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
            });

        Application? application6 = await _applicationRepository.FirstOrDefaultAsync(s => s.ProjectName == "Village Fountain Repair");
        application6 ??= await _applicationRepository.InsertAsync(
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
            });

        Application? application7 = await _applicationRepository.FirstOrDefaultAsync(s => s.ProjectName == "Hoover");
        application7 ??= await _applicationRepository.InsertAsync(
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
            });

        Application? application8 = await _applicationRepository.FirstOrDefaultAsync(s => s.ProjectName == "Tree Planting");
        application8 ??= await _applicationRepository.InsertAsync(
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
            });

        Application? application9 = await _applicationRepository.FirstOrDefaultAsync(s => s.ProjectName == "Pizza Joint");
        application9 ??= await _applicationRepository.InsertAsync(
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
            });

        Application? application10 = await _applicationRepository.FirstOrDefaultAsync(s => s.ProjectName == "Froghopper Express");
        application10 ??= await _applicationRepository.InsertAsync(
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
            });

        Application? application11 = await _applicationRepository.FirstOrDefaultAsync(s => s.ProjectName == "Courtyard Landscaping");
        application11 ??= await _applicationRepository.InsertAsync(
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
            });

        Application? application12 = await _applicationRepository.FirstOrDefaultAsync(s => s.ProjectName == "Disco Ball");
        application12 ??= await _applicationRepository.InsertAsync(
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
            });

        Application? application13 = await _applicationRepository.FirstOrDefaultAsync(s => s.ProjectName == "Gymnasium Repair");
        application13 ??= await _applicationRepository.InsertAsync(
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
            });

        Application? application14 = await _applicationRepository.FirstOrDefaultAsync(s => s.ProjectName == "Holiday Abroad Funding");
        application14 ??= await _applicationRepository.InsertAsync(
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
            });

        ApplicationUserAssignment? appUserAssignment1 = await _applicationUserAssignmentRepository.FirstOrDefaultAsync(s => s.OidcSub == "12345");
        appUserAssignment1 ??= await _applicationUserAssignmentRepository.InsertAsync(
            new ApplicationUserAssignment
            {
                OidcSub = "12345",
                ApplicationFormId = appForm1.Id,
                ApplicationId = application1.Id,
                AssigneeDisplayName = "John Smith",
                AssignmentTime = new DateTime(2023, 01, 02)
            });

        ApplicationUserAssignment? appUserAssignment2 = await _applicationUserAssignmentRepository.FirstOrDefaultAsync(s => s.OidcSub == "3456");
        appUserAssignment2 ??= await _applicationUserAssignmentRepository.InsertAsync(
            new ApplicationUserAssignment
            {
                OidcSub = "3456",
                ApplicationFormId = appForm2.Id,
                ApplicationId = application2.Id,
                AssigneeDisplayName = "Will Smith",
                AssignmentTime = new DateTime(2023, 02, 02)
            });

        ApplicationUserAssignment? appUserAssignment31 = await _applicationUserAssignmentRepository.FirstOrDefaultAsync(s => s.OidcSub == "23564");
        appUserAssignment31 ??= await _applicationUserAssignmentRepository.InsertAsync(
            new ApplicationUserAssignment
            {
                OidcSub = "23564",
                ApplicationFormId = appForm2.Id,
                ApplicationId = application3.Id,
                AssigneeDisplayName = "John Doe",
                AssignmentTime = new DateTime(2023, 02, 02)
            });

        ApplicationUserAssignment? appUserAssignment32 = await _applicationUserAssignmentRepository.FirstOrDefaultAsync(s => s.OidcSub == "76857");
        appUserAssignment32 ??= await _applicationUserAssignmentRepository.InsertAsync(
            new ApplicationUserAssignment
            {
                OidcSub = "76857",
                ApplicationFormId = appForm1.Id,
                ApplicationId = application3.Id,
                AssigneeDisplayName = "Joe Wilson",
                AssignmentTime = new DateTime(2023, 04, 04)
            });

        ApplicationUserAssignment? appUserAssignment41 = await _applicationUserAssignmentRepository.FirstOrDefaultAsync(s => s.OidcSub == "38332");
        appUserAssignment41 ??= await _applicationUserAssignmentRepository.InsertAsync(
            new ApplicationUserAssignment
            {
                OidcSub = "38332",
                ApplicationFormId = appForm2.Id,
                ApplicationId = application4.Id,
                AssigneeDisplayName = "Eva Harris",
                AssignmentTime = new DateTime(2023, 05, 02)
            });

        ApplicationUserAssignment? appUserAssignment42 = await _applicationUserAssignmentRepository.FirstOrDefaultAsync(s => s.OidcSub == "76857");
        appUserAssignment42 ??= await _applicationUserAssignmentRepository.InsertAsync(
            new ApplicationUserAssignment
            {
                OidcSub = "777888",
                ApplicationFormId = appForm1.Id,
                ApplicationId = application4.Id,
                AssigneeDisplayName = "Michael John",
                AssignmentTime = new DateTime(2023, 06, 06)
            });

        ApplicationUserAssignment? appUserAssignment43 = await _applicationUserAssignmentRepository.FirstOrDefaultAsync(s => s.OidcSub == "764658");
        appUserAssignment43 ??= await _applicationUserAssignmentRepository.InsertAsync(
            new ApplicationUserAssignment
            {
                OidcSub = "764658",
                ApplicationFormId = appForm1.Id,
                ApplicationId = application4.Id,
                AssigneeDisplayName = "Kevin Douglas",
                AssignmentTime = new DateTime(2023, 07, 07)
            });


        // Seed some application users for testing
        var identityUser1 = await _identityUserManager.FindByEmailAsync("steve.rogers@example.com");
        if (identityUser1 == null)
        {
            identityUser1 = new(Guid.NewGuid(), "steve.rogers", "steve.rogers@example.com")
            {
                Name = "Steve",
                Surname = "Rogers"
            };
            await _identityUserManager.CreateAsync(identityUser1);
        }

        var identityUser2 = await _identityUserManager.FindByEmailAsync("bruce.banner@example.com");
        if (identityUser2 == null)
        {
            identityUser2 = new(Guid.NewGuid(), "bruce.banner", "testuser2@example.com")
            {
                Name = "Bruce",
                Surname = "Banner"
            };
            await _identityUserManager.CreateAsync(identityUser2);
        }

        var identityUser3 = await _identityUserManager.FindByEmailAsync("natasha.romanoff@example.com");
        if (identityUser3 == null)
        {
            identityUser3 = new(Guid.NewGuid(), "natasha.romanoff", "testuser3@example.com")
            {
                Name = "Natasha",
                Surname = "Romanoff"
            };
            await _identityUserManager.CreateAsync(identityUser3);
        };
    }
}
