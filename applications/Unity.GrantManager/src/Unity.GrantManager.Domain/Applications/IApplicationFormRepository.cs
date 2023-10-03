using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IApplicationFormRepository : IRepository<ApplicationForm, Guid>
{
}
