
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Unity.GrantManager.Intake;

namespace Unity.GrantManager;

public class ConfigureIntakeClientOptions : IConfigureOptions<IntakeClientOptions>
{
    private readonly IConfiguration _configuration;
    const string PROTOCOL = "https://";
    const string DefaultBaseUri = $"{PROTOCOL}submit.digital.gov.bc.ca/app/api/v1";


    public ConfigureIntakeClientOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public void Configure(IntakeClientOptions options)
    {
        options.BaseUri = DefaultBaseUri;
        options.BearerTokenPlaceholder = _configuration["Intake:BearerTokenPlaceholder"] ?? "";
        options.UseBearerToken = _configuration.GetValue<bool>("Intake:UseBearerToken");
        options.AllowUnregisteredVersions = _configuration.GetValue<bool>("Intake:AllowUnregisteredVersions");
    }
}

