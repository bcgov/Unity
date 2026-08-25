using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.DataSeed;

public class AIDataSeeder(
    AIPromptDataSeeder promptDataSeeder,
    AIModelDataSeeder modelDataSeeder,
    AIOperationDataSeeder operationDataSeeder) : IDataSeedContributor, ITransientDependency
{
    public async Task SeedAsync(DataSeedContext context)
    {
        await promptDataSeeder.SeedAsync(context);
        await modelDataSeeder.SeedAsync(context);
        await operationDataSeeder.SeedAsync(context);
    }
}
