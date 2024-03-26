using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationContactService : ICrudAppService<
            ApplicationContactDto,
            Guid>
{
    Task<List<ApplicationContactDto>> GetListByApplicationAsync(Guid applicationId);
    
}
