namespace Unity.Modules.Shared.PostTenantCreation;

/// <summary>
/// Result of <see cref="IPostTenantCreationStep.CanExecuteAsync"/>: run the step, or record why it
/// did not run as <see cref="PostTenantCreationStepStatus.Skipped"/> or
/// <see cref="PostTenantCreationStepStatus.Failure"/>.
/// </summary>
public sealed class PostTenantCreationStepCheck
{
    private PostTenantCreationStepCheck(bool canExecute, PostTenantCreationStepStatus? status, string? reason)
    {
        CanExecute = canExecute;
        Status = status;
        Reason = reason;
    }

    /// <summary>The step's preconditions are met - run it.</summary>
    public static PostTenantCreationStepCheck Run { get; } = new(true, null, null);

    public bool CanExecute { get; }

    /// <summary>Skipped or Failure when <see cref="CanExecute"/> is false; null otherwise.</summary>
    public PostTenantCreationStepStatus? Status { get; }

    /// <summary>Why the step did not run - persisted as the step's status message.</summary>
    public string? Reason { get; }

    /// <summary>The step does not apply to this tenant or environment.</summary>
    public static PostTenantCreationStepCheck Skip(string reason) =>
        new(false, PostTenantCreationStepStatus.Skipped, reason);

    /// <summary>The step should apply but cannot run, e.g. required configuration is missing.</summary>
    public static PostTenantCreationStepCheck Fail(string reason) =>
        new(false, PostTenantCreationStepStatus.Failure, reason);
}
