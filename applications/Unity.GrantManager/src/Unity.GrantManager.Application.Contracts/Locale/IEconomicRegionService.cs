using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Locale;

public interface IEconomicRegionService : IApplicationService
{
    Task<IList<EconomicRegionDto>> GetListAsync();
}

