using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IFundingHistoryRepository : IRepository<FundingHistory, Guid>
{
    Task<List<FundingHistory>> GetByApplicantIdAsync(Guid applicantId);
}
