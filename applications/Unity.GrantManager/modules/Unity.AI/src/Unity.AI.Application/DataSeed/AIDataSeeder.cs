using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.DataSeed;

/// <summary>
/// Seeds the built-in AI prompts (insert-only). AI models and operations are environment
/// configuration, managed in Unity Admin -> AI Setup, and are never created or changed here.
/// </summary>
public class AIDataSeeder(
    AIPromptDataSeeder promptDataSeeder) : IDataSeedContributor, ITransientDependency
{
    public async Task SeedAsync(DataSeedContext context)
    {
        await promptDataSeeder.SeedAsync(context);
    }
}
