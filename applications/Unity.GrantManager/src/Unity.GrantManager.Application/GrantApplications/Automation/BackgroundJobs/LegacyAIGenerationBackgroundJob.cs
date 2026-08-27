using System.Threading.Tasks;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

// Temporary deployment bridge. Remove this base and the six legacy adapters after
// pre-generic BackgroundJobs rows have drained; new work must use the generic job.
public abstract class LegacyAIGenerationBackgroundJob<TArgs>(
    AIGenerationBackgroundJob genericJob)
    : AsyncBackgroundJob<TArgs>, ITransientDependency
{
    protected abstract AIGenerationBackgroundJobArgs Convert(TArgs args);

    public override Task ExecuteAsync(TArgs args) => genericJob.ExecuteAsync(Convert(args));
}
