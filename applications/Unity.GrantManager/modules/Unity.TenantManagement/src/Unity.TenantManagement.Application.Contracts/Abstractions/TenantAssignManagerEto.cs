using System;
using Volo.Abp.EventBus;

namespace Unity.TenantManagement.Abstractions
{
    [Serializable]
    [EventName("abp.multi_tenancy.tenant.manager.assignment")]
    public class TenantAssignManagerEto
    {
        public string UserIdentifier { get; set; } = null;
        public Guid TenantId { get; set; }
    }
}
