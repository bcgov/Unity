using System;

namespace Unity.Modules.Shared.PostTenantCreation;

/// <summary>
/// A single entry in the "Sections" node persisted to <c>Tenant.ExtraProperties</c>, recording
/// the latest status of one <see cref="IPostTenantCreationStep"/> for that tenant.
/// </summary>
public class PostTenantCreationStepStatusEntry
{
    /// <summary>Matches <see cref="IPostTenantCreationStep.Key"/>.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Matches <see cref="IPostTenantCreationStep.StepName"/> at the time of the last update.</summary>
    public string Name { get; set; } = string.Empty;

    public PostTenantCreationStepStatus Status { get; set; } = PostTenantCreationStepStatus.Waiting;

    /// <summary>Error detail or reason when <see cref="Status"/> is <see cref="PostTenantCreationStepStatus.Error"/>, <see cref="PostTenantCreationStepStatus.Failure"/> or <see cref="PostTenantCreationStepStatus.Skipped"/>.</summary>
    public string? Message { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
