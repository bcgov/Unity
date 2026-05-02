using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IReportsHistoryRepository : IRepository<ReportsHistory, Guid>
{
    Task<List<ReportsHistory>> GetByApplicantIdAsync(Guid applicantId);
}
