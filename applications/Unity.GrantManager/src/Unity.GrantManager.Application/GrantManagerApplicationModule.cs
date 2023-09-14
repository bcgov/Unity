using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.Intake;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.BlobStoring;
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
        var configuration = context.Services.GetConfiguration();
        Configure<AbpBlobStoringOptions>(options =>
        {
            options.Containers.Configure<ComsS3Container>(container =>
            {
                container.UseComsS3CustomBlobProvider(provider =>
                {
                    provider.AccessKeyId = configuration["S3:AccessKeyId"] ?? "";
                    provider.Bucket = configuration["S3:Bucket"] ?? "";
                    provider.Endpoint = configuration["S3:Endpoint"] ?? "";
                    provider.SecretAccessKey = configuration["S3:SecretAccessKey"] ?? "";
                    provider.ApplicationS3Folder = configuration["S3:ApplicationS3Folder"] ?? "";
                    provider.AdjudicationS3Folder = configuration["S3:AdjudicationS3Folder"] ?? "";
                    provider.BaseUri = configuration["S3:BaseUri"] ?? "";
                    provider.Username = configuration["S3:Username"] ?? "";
                    provider.Password = configuration["S3:Password"] ?? "";
                });
            });
        });
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<GrantManagerApplicationModule>();
        });

        Configure<IntakeClientOptions>(options =>
        {
            options.BaseUri = configuration["Intake:BaseUri"] ?? "";
            options.BearerTokenPlaceholder = configuration["Intake:BearerTokenPlaceholder"] ?? "";
            options.UseBearerToken = configuration.GetValue<bool>("Intake:UseBearerToken");
        });

        context.Services.AddSingleton<RestClient>(provider =>
        {
            var options = provider.GetService<IOptions<IntakeClientOptions>>()?.Value;
            if (null != options)
            {
                var restOptions = new RestClientOptions(options.BaseUri)
                {
                    // NOTE: Basic authentication only works for fetching forms and lists of form submissions
                    // Authenticator = options.UseBearerToken ?
                    //    new JwtAuthenticator(options.BearerTokenPlaceholder) :
                    //    new HttpBasicAuthenticator(options.FormId, options.ApiKey),

                    FailOnDeserializationError = true,
                    ThrowOnDeserializationError = true
                };

                return new RestClient(
                    restOptions,
                    configureSerialization: s =>
                        s.UseSystemTextJson(new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true,
                            PropertyNameCaseInsensitive = true,
                            ReadCommentHandling = JsonCommentHandling.Skip,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        })
                    );
            }
            else
            {
                return new RestClient(
                    configureSerialization: s =>
                        s.UseSystemTextJson(new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true,
                            PropertyNameCaseInsensitive = true,
                            ReadCommentHandling = JsonCommentHandling.Skip,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        })
                    );
            }
        });
    }
}