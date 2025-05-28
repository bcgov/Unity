using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Zones;
public interface IZoneManagementAppService : IApplicationService
{
    Task<ZoneGroupDefinitionDto> GetAsync(string providerName, string providerKey);
    Task<HashSet<string>> GetZoneStateSetAsync(Guid formId);
    Task SetAsync(string providerName, string providerKey, List<UpdateZoneDto> input);
}
