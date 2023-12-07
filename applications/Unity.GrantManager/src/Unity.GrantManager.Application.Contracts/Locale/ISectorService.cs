using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Locale;

public interface ISectorService : IApplicationService
{
    Task<IList<SectorDto>> GetListAsync();
}

public interface IApplicationSubSectorService : IApplicationService
{
    Task<IList<SectorDto>> GetListAsync();
}
