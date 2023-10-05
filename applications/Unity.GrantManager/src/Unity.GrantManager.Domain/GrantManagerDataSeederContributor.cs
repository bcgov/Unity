using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantPrograms;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Unity.GrantManager;

public class GrantManagerDataSeederContributor
    : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<GrantProgram, Guid> _grantProgramRepository;
    private readonly IRepository<Intake, Guid> _intakeRepository;
    private readonly IRepository<ApplicationForm, Guid> _applicationFormRepository;
    private readonly IRepository<Applicant, Guid> _applicantRepository;
    private readonly IApplicationStatusRepository _applicationStatusRepository;
    private readonly IdentityUserManager _identityUserManager;

    public GrantManagerDataSeederContributor(IRepository<GrantProgram, Guid> grantProgramRepository,
        IRepository<Intake, Guid> intakeRepository,
        IRepository<ApplicationForm, Guid> applicationFormRepository,
        IRepository<Applicant, Guid> applicantRepository,
        IApplicationStatusRepository applicationStatusRepository,
        IdentityUserManager identityUserManager)
    {
        _grantProgramRepository = grantProgramRepository;
        _intakeRepository = intakeRepository;
        _applicationFormRepository = applicationFormRepository;
        _applicantRepository = applicantRepository;
        _applicationStatusRepository = applicationStatusRepository;
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
                    PublishDate = new DateTime(2023, 6, 8, 0, 0, 0, DateTimeKind.Utc),
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
                     PublishDate = new DateTime(2023, 5, 15, 0, 0, 0, DateTimeKind.Utc),
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
                    PublishDate = new DateTime(2020, 5, 15, 0, 0, 0, DateTimeKind.Utc),
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
                    PublishDate = new DateTime(1992, 01, 01, 0, 0, 0, DateTimeKind.Utc),
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
                    PublishDate = new DateTime(2002, 01, 01, 0, 0, 0, DateTimeKind.Utc),
                },
                autoSave: true
            );
        }

        Intake? spaceFarmsIntake1 = await _intakeRepository.FirstOrDefaultAsync(s => s.IntakeName == "2022 Intake");
        spaceFarmsIntake1 ??= await _intakeRepository.InsertAsync(
                new Intake
                {
                    IntakeName = "2022 Intake",
                    StartDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                autoSave: true
        );

        Intake? spaceFarmsIntake2 = await _intakeRepository.FirstOrDefaultAsync(s => s.IntakeName == "2023 Intake");
        spaceFarmsIntake2 ??= await _intakeRepository.InsertAsync(
            new Intake
            {
                IntakeName = "2023 Intake",
                StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
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

        ApplicationStatus? status8 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == ApplicationStatusConsts.UNDER_ASSESSMENT);
        status8 ??= await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "UNDER_ASSESSMENT",
                ExternalStatus = "Under Review",
                InternalStatus = "Under Assessment"
            }
        );

        ApplicationStatus? status9 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == ApplicationStatusConsts.ASSESSMENT_COMPLETED);
        status9 ??= await _applicationStatusRepository.InsertAsync(
            new ApplicationStatus
            {
                StatusCode = "ASSESSMENT_COMPLETED",
                ExternalStatus = "Under Review",
                InternalStatus = "Assessment Completed"
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
        }
    }
}