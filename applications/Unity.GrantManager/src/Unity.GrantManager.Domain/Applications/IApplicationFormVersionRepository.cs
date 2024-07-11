using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IApplicationFormVersionRepository : IRepository<ApplicationFormVersion, Guid>
{
    Task<ApplicationFormVersion> GetByChefsFormVersionAsync(Guid chefsFormVersionId);
}
