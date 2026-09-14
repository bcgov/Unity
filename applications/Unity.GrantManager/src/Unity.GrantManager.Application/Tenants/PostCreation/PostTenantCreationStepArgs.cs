using System;

namespace Unity.GrantManager.Tenants.PostCreation;

public class PostTenantCreationStepArgs
{
    public Guid TenantId { get; set; }

    /// <summary>Index into the ordered <see cref="Unity.Modules.Shared.PostTenantCreation.IPostTenantCreationStep"/>
    /// list of the step to run next.</summary>
    public int StepIndex { get; set; }
}
