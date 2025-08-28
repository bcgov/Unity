using Microsoft.Extensions.Configuration;
using Unity.GrantManager.Intake;
using Unity.GrantManager.Integrations;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.GrantManager;

public class ConfigureIntakeClientOptions(
                                        IConfiguration configuration,
                                        IEndpointManagementAppService endpointManagementAppService)
{
    public async Task ConfigureAsync(IntakeClientOptions options, CancellationToken cancellationToken = default)
    {
        var intakeBaseUri = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.INTAKE_API_BASE);
        options.BaseUri = intakeBaseUri;
        options.BearerTokenPlaceholder = configuration["Intake:BearerTokenPlaceholder"] ?? "";
        options.UseBearerToken = configuration.GetValue<bool>("Intake:UseBearerToken");
        options.AllowUnregisteredVersions = configuration.GetValue<bool>("Intake:AllowUnregisteredVersions");
    }
}

