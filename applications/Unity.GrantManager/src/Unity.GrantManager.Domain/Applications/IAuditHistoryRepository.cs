using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IAuditHistoryRepository : IRepository<AuditHistory, Guid>
{
    Task<List<AuditHistory>> GetByApplicantIdAsync(Guid applicantId);
}
