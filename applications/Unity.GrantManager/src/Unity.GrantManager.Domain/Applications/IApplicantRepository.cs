using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IApplicantRepository : IRepository<Applicant, Guid>
{
    Task<List<Applicant>> GetUnmatchedApplicants();
    Task<Applicant?> GetByUnityApplicantIdAsync(string unityApplicantId);
    Task<Applicant?> GetByUnityApplicantNameAsync(string unityApplicantName);
}
