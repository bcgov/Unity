using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantPrograms;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager;

public class GrantManagerDataSeederContributor
    : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<GrantProgram, Guid> _grantProgramRepository;

    public GrantManagerDataSeederContributor(IRepository<GrantProgram, Guid> grantProgramRepository)
    {
        _grantProgramRepository = grantProgramRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _grantProgramRepository.GetCountAsync() > 0)
        {
            return;
        }

        await _grantProgramRepository.InsertAsync(
            new GrantProgram
            {
                ProgramName = "Space Farms Grant Program",
                Type = GrantProgramType.Agriculture,
                PublishDate = new DateTime(2023, 6, 8),
            },
            autoSave: true
        );

        await _grantProgramRepository.InsertAsync(
            new GrantProgram
            {
                ProgramName = "Fictional Arts Accelerator Grant",
                Type = GrantProgramType.Arts,
                PublishDate = new DateTime(2023, 5, 15),
            },
            autoSave: true
        );

        await _grantProgramRepository.InsertAsync(
            new GrantProgram
            {
                ProgramName = "New Approaches in Counting Grant",
                Type = GrantProgramType.Research,
                PublishDate = new DateTime(2020, 5, 15),
            },
            autoSave: true
        );

        await _grantProgramRepository.InsertAsync(
            new GrantProgram
            {
                ProgramName = "BizBusiness Fund",
                Type = GrantProgramType.Business,
                PublishDate = new DateTime(1992, 01, 01),
            },
            autoSave: true
        );

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
}
