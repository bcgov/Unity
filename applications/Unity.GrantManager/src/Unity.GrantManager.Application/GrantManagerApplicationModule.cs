using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Serializers.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.Intake;
using Unity.GrantManager.Integrations.Sso;
using Unity.TenantManagement;
using Volo.Abp.AutoMapper;
using Volo.Abp.BlobStoring;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager;

[DependsOn(
    typeof(GrantManagerDomainModule),
    typeof(GrantManagerApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(UnityTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(AbpBackgroundWorkersQuartzModule)
    )]
[DependsOn(typeof(AbpBackgroundWorkersQuartzModule))]
    public class GrantManagerApplicationModule : AbpModule
{
    //Set some defaults 

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<AbpBlobStoringOptions>(options =>
        {
            options.Containers.Configure<S3Container>(container =>
            {
                container.UseS3CustomBlobProvider(provider =>
                {
                    provider.AccessKeyId = configuration["S3:AccessKeyId"] ?? "";
                    provider.Bucket = configuration["S3:Bucket"] ?? "";
                    provider.Endpoint = configuration["S3:Endpoint"] ?? "";
                    provider.SecretAccessKey = configuration["S3:SecretAccessKey"] ?? "";
                    provider.ApplicationS3Folder = configuration["S3:ApplicationS3Folder"] ?? "";
                    provider.AssessmentS3Folder = configuration["S3:AssessmentS3Folder"] ?? "";
                });
            });
        });
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<GrantManagerApplicationModule>();
        });

        context.Services.AddSingleton<IAuthorizationHandler, AssessmentAuthorizationHandler>();

        Configure<IntakeClientOptions>(options =>
        {
            options.BaseUri = configuration["Intake:BaseUri"] ?? "";
            options.BearerTokenPlaceholder = configuration["Intake:BearerTokenPlaceholder"] ?? "";
            options.UseBearerToken = configuration.GetValue<bool>("Intake:UseBearerToken");
            options.AllowUnregisteredVersions = configuration.GetValue<bool>("Intake:AllowUnregisteredVersions");
        });


        context.Services.Configure<CssApiOptions>(
            configuration.GetSection(
                key: "CssApi"));

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

        // Set the max defaults as max - we are using non serverside paging and this effect this
        PagedAndSortedResultRequestDto.DefaultMaxResultCount = int.MaxValue;
        PagedAndSortedResultRequestDto.MaxMaxResultCount = int.MaxValue;
    }
}