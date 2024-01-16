using System;

namespace Unity.GrantManager.Identity
{
    public class UserTenantAccountDto
    {
        public Guid Id { get; set; }
        public Guid? TenantId { get; set; }
        public string? TenantName { get; set; } = null;
        public string Username { get; set; } = string.Empty;
        public string? DisplayName { get; set; } = null;
        public string OidcSub { get; set; } = string.Empty;
    }
}
