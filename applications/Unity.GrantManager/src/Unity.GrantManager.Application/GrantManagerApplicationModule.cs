using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.Json;
using System;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.GrantManager.Intake;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager;

[DependsOn(
    typeof(GrantManagerDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(GrantManagerApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class GrantManagerApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<GrantManagerApplicationModule>();
        });

        var configuration = context.Services.GetConfiguration();

        Configure<IntakeClientOptions>(options => {
            options.BaseUri = configuration["Intake:BaseUri"] ?? "";
            options.FormId  = configuration["Intake:FormId"] ?? "";
            options.ApiKey  = configuration["Intake:ApiKey"] ?? "";
            options.BearerTokenPlaceholder = configuration["Intake:BearerTokenPlaceholder"] ?? "";
            options.UseBearerToken = configuration.GetValue<bool>("Intake:UseBearerToken");
        });

        context.Services.AddSingleton<RestClient>(provider =>
        {
            var options = provider.GetService<IOptions<IntakeClientOptions>>().Value;

            var restOptions = new RestClientOptions(options.BaseUri)
            {
                // NOTE: Basic authentication only works for fetching forms and lists of form submissions
                Authenticator = options.UseBearerToken ?
                    new JwtAuthenticator(options.BearerTokenPlaceholder) :
                    new HttpBasicAuthenticator(options.FormId, options.ApiKey),

                FailOnDeserializationError = true,
                ThrowOnDeserializationError = true
            };

            var client = new RestClient(
                restOptions,
                configureSerialization: s => 
                    s.UseSystemTextJson(new System.Text.Json.JsonSerializerOptions {
                        WriteIndented = true,
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    })
                );

            return client;
        });
    }
}
