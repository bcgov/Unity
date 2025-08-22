using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Unity.GrantManager.Intake;
using Unity.GrantManager.Integrations;

namespace Unity.GrantManager;

public class ConfigureIntakeClientOptions(
                                        IConfiguration configuration,
                                        IEndpointManagementAppService endpointManagementAppService) : IConfigureOptions<IntakeClientOptions>
{   
    public void Configure(IntakeClientOptions options)
    {
        // Note: GetUgmUrlByKeyNameAsync is async, but IConfigureOptions.Configure must be sync.
        // If possible, use a sync alternative or ensure the value is available synchronously.
        // Here, we block on the async call (not ideal, but sometimes necessary in options pattern).
        var intakeBaseUri = endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.INTAKE_API_BASE).GetAwaiter().GetResult();
        options.BaseUri = intakeBaseUri;
        options.BearerTokenPlaceholder = configuration["Intake:BearerTokenPlaceholder"] ?? "";
        options.UseBearerToken = configuration.GetValue<bool>("Intake:UseBearerToken");
        options.AllowUnregisteredVersions = configuration.GetValue<bool>("Intake:AllowUnregisteredVersions");
    }
}

