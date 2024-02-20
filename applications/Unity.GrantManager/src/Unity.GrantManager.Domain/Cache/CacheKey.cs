using System;

namespace Unity.GrantManager.Cache;

public class CacheKey
{
    public string CacheType { get; set; } = string.Empty;
    public Guid TenantGuid { get; set; }

    public override string ToString()
    {
        return $"{CacheType}_{TenantGuid}";
    }
}
