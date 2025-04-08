using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.Zones;
public interface IZoneChecker
{
    Task<bool> IsEnabledAsync(string name);
    Task<HashSet<string>?> GetOrNullAsync(string name);
}
