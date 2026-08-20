using System;
using System.Threading.Tasks;

namespace Unity.Modules.Shared.PostTenantCreation;

/// <summary>
/// A single step in the post-tenant-creation sequence, run as part of
/// <c>PostTenantCreationSequenceJob</c> after a new tenant is created. Implement this in any
/// module and register it as an <see cref="Volo.Abp.DependencyInjection.ITransientDependency"/> to
/// have it picked up automatically - no changes to the sequencing job are needed.
/// </summary>
public interface IPostTenantCreationStep
{
    /// <summary>Determines execution order relative to other steps (ascending).</summary>
    int Order { get; }

    /// <summary>Short, human-readable name used in logging.</summary>
    string StepName { get; }

    /// <summary>
    /// When true, a failure in this step is logged and the sequence continues to the next step.
    /// When false, a failure stops the sequence - later steps do not run.
    /// </summary>
    bool ContinueOnError { get; }

    Task ExecuteAsync(Guid tenantId);
}
