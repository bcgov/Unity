using System;

namespace Unity.GrantManager.Caching
{
    public class LocalityCacheKey
    {
        public LocalityCacheKey(string? type, Guid? tenantGuid)
        {
            Type = type;
            TenantGuid = tenantGuid;
        }

        public string? Type { get; set; }
        public Guid? TenantGuid { get; set; }

        public override string ToString()
        {
            return $"{Type}_{TenantGuid}";
        }
    }
}
