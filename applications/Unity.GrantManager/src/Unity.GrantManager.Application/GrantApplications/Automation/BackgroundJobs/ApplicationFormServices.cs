using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

/// <summary>
/// Aggregates the application and form repositories used by <see cref="RunApplicationAIPipelineJob"/>.
/// Introduced to keep the constructor within SonarQube's 7-parameter limit (S107).
/// Implements <see cref="ITransientDependency"/> so ABP's DI scanning registers it in the container.
/// </summary>
public record ApplicationFormServices(
    IApplicationRepository Application,
    IApplicationFormRepository ApplicationForm) : ITransientDependency;
