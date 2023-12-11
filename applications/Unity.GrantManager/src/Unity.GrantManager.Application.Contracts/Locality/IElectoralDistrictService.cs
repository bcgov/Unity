using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Locality;

public interface IElectoralDistrictService : IApplicationService
{
    Task<IList<ElectoralDistrictDto>> GetListAsync();
}

