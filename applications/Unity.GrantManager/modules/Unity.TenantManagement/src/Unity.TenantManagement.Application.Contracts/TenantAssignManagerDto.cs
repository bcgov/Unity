using System;

namespace Unity.TenantManagement
{
    public class TenantAssignManagerDto
    {
        public Guid TenantId { get; set; }
        public string UserIdentifier { get; set; }
    }
}
