using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IApplicationFormSubmissionRepository : IRepository<ApplicationFormSubmission, Guid>
{
}
