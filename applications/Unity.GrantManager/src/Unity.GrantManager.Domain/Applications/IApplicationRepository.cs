using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantPrograms;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IApplicationRepository : IRepository<Application, Guid>
{
    Task<List<Application>> GetListAsync(
        int skipCount,
        int maxResultCount,
        string sorting,
        string filter = null
    );
   
}
