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

     public GrantManagerDataSeederContributor(IRepository<GrantProgram, Guid> grantProgramRepository, 
         IIntakeRepository intakeRepository, 
         IApplicationFormRepository applicationFormRepository)
     {
         _grantProgramRepository = grantProgramRepository;
         _intakeRepository = intakeRepository;
         _applicationFormRepository = applicationFormRepository;
     }
       

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _grantProgramRepository.GetCountAsync() > 0)
        {
            return;
        }

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
                GrantProgramId = spaceFarms.Id,
                IntakeName = "Space Farms Intake 1"
            },
            autoSave: true
        );

        var spaceFarmsIntake2 = await _intakeRepository.InsertAsync(
            new Intake
            {
                GrantProgramId = spaceFarms.Id,
                IntakeName = "Space Farms Intake 2"
            },
            autoSave: true
        );

        var appForm1 = await _applicationFormRepository.InsertAsync(
            new ApplicationForm
            {
                IntakeId = spaceFarmsIntake1.Id,
                ApplicationFormName = "Space Farms Intake 1 Form 1"
            },
            autoSave: true
        );

        var appForm2 = await _applicationFormRepository.InsertAsync(
            new ApplicationForm
            {
                IntakeId = spaceFarmsIntake1.Id,
                ApplicationFormName = "Space Farms Intake 1 Form 2"
            },
            autoSave: true
        ); 
       
    }
}
