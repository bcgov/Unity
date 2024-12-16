using System;

namespace Unity.GrantManager.Reporting
{
    public class DynamicViewGenerationArgs
    {
        public Guid ApplicationFormVersionId { get; set; }
        public Guid? TenantId { get; set; }
    }
}
