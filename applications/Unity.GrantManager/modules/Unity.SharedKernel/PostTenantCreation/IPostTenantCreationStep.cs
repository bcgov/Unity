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

    /// <summary>
    /// Stable, machine-readable identifier for this step (e.g. "MetabaseSync"). Used as the key
    /// for the step's persisted status entry (see <c>TenantPostCreationSectionsExtensions</c>) -
    /// unlike <see cref="StepName"/>, this must not change once shipped, since it is what
    /// correlates a status entry already stored on a tenant with this step on later runs.
    /// </summary>
    string Key { get; }

    /// <summary>Short, human-readable name used in logging and shown to admins in the UI.</summary>
    string StepName { get; }

    /// <summary>
    /// When true, a failure in this step is logged and the sequence continues to the next step.
    /// When false, a failure stops the sequence - later steps do not run.
    /// </summary>
    bool ContinueOnError { get; }

    /// <summary>
    /// Validated before <see cref="ExecuteAsync"/> runs. When false, the step is skipped (logged,
    /// not treated as a failure) and the sequence moves on to the next step. Defaults to true -
    /// override to check preconditions such as required configuration being present.
    /// </summary>
    Task<bool> CanExecuteAsync(Guid tenantId) => Task.FromResult(true);

    Task ExecuteAsync(Guid tenantId);
}
