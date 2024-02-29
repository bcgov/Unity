using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationContactService : IApplicationService
{
    Task<List<ApplicationContactDto>> GetListAsync(Guid applicationId);

    Task<ApplicationContactDto> GetAsync(Guid id);

    Task<ApplicationContactDto> CreateAsync(ApplicationContactDto input);

    Task<ApplicationContactDto> UpdateAsync(ApplicationContactDto input);
    
}
