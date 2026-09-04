namespace Unity.Modules.Shared.PostTenantCreation;

/// <summary>
/// Result of a single <see cref="IPostTenantCreationStep"/>, persisted on the tenant so it can be
/// displayed in the Tenants admin UI (see <see cref="TenantPostCreationSectionsExtensions"/>).
/// </summary>
public enum PostTenantCreationStepStatus
{
    /// <summary>Not yet run - the default status seeded when the tenant is created.</summary>
    Waiting,

    /// <summary><see cref="IPostTenantCreationStep.ExecuteAsync"/> completed without error.</summary>
    Success,

    /// <summary>The step ran but reported a handled (non-exception) failure.</summary>
    Failure,

    /// <summary><see cref="IPostTenantCreationStep.ExecuteAsync"/> threw an exception.</summary>
    Error
}
