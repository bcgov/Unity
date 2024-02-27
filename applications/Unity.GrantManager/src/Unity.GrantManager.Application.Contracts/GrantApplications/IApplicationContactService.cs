using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationContactService : IApplicationService
{
    Task<IList<ApplicationContactDto>> GetListAsync();

    Task<ApplicationContactDto> CreateAsync(ApplicationContactDto input);
}
