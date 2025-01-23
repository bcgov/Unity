using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Unity.GrantManager.Zones;
public interface IZoneManagementAppService
{
    Task<HashSet<string>> GetZoneStateSet(Guid formId);
}
