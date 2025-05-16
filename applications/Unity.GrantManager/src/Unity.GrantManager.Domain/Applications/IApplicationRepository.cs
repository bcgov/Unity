using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IApplicationRepository : IRepository<Application, Guid>
{
    Task<Application> WithBasicDetailsAsync(Guid id);
    Task<Application?> GetWithFullDetailsByIdAsync(Guid id);
    Task<List<IGrouping<Guid, Application>>> WithFullDetailsGroupedAsync(int skipCount, int maxResultCount, string? sorting = null, string? filter = null);
    Task<List<Application>> GetListByIdsAsync(Guid[] ids);
}
