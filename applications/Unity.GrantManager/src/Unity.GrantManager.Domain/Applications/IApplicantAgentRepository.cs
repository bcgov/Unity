using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IApplicantAgentRepository : IRepository<ApplicantAgent, Guid>
{
    Task<ApplicantAgent?> GetByApplicantIdAsync(Guid applicantId);
    Task<List<ApplicantAgent>> GetListByApplicantIdAsync(Guid applicantId);
}
