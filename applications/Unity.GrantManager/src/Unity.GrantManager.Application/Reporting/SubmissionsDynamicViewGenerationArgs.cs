using System;

namespace Unity.GrantManager.Reporting
{
    public class SubmissionsDynamicViewGenerationArgs
    {
        public Guid ApplicationFormVersionId { get; set; }
        public Guid? TenantId { get; set; }
    }
}
