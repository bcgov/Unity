using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IApplicantAddressRepository : IBasicRepository<ApplicantAddress, Guid>
{
    Task<List<ApplicantAddress>> FindByApplicantIdAsync(Guid applicantId);
}
