using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Settings;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public record AIIntakePipelineGuardServices(
    ISettingProvider SettingProvider,
    IApplicationRepository Application,
    IApplicationFormRepository ApplicationForm) : ITransientDependency;
