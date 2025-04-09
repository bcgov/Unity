using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.Zones;
public interface IZoneChecker
{
    Task<bool> IsEnabledAsync(string name, Guid formId);
    Task<HashSet<string>?> GetOrNullAsync(string name, Guid formId);
}
