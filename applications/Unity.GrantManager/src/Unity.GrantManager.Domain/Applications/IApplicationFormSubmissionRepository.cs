using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IApplicationFormSubmissionRepository : IRepository<ApplicationFormSubmission, Guid>
{
    Task<ApplicationFormSubmission> GetByApplicationAsync(Guid applicationId);
}
