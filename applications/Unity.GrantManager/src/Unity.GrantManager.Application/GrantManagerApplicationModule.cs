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
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Dtos;
using Unity.Notifications;
using Unity.Notifications.Integrations.Ches;
using Unity.GrantManager.Intakes.BackgroundWorkers;
using Unity.Payments.Integrations.Cas;
using Unity.Flex;
using Unity.Payments;

namespace Unity.GrantManager;

[DependsOn(
    typeof(GrantManagerDomainModule),
    typeof(GrantManagerApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(UnityTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(AbpBackgroundWorkersQuartzModule),
    typeof(AbpBackgroundWorkersQuartzModule),
    typeof(NotificationsApplicationModule),    
    typeof(PaymentsApplicationModule),
    typeof(FlexApplicationModule)
    )]
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
        context.Services.AddSingleton<IAuthorizationHandler, ApplicationAuthorizationHandler>();

        Configure<IntakeClientOptions>(options =>
        {
            // This fails unit tests unless set to a non empty string
            // RestClient will throw an error - baseUrl can not be empty
            options.BaseUri = configuration["Intake:BaseUri"] ?? "https://submit.digital.gov.bc.ca/app/api/v1";
            options.BearerTokenPlaceholder = configuration["Intake:BearerTokenPlaceholder"] ?? "";
            options.UseBearerToken = configuration.GetValue<bool>("Intake:UseBearerToken");
            options.AllowUnregisteredVersions = configuration.GetValue<bool>("Intake:AllowUnregisteredVersions");
        });

        context.Services.Configure<CasClientOptions>(configuration.GetSection(key: "Payments"));
        context.Services.Configure<CssApiOptions>(configuration.GetSection(key: "CssApi"));
        context.Services.Configure<ChesClientOptions>(configuration.GetSection(key: "Notifications"));
        Configure<BackgroundJobsOptions>(options =>
        {
            options.IsJobExecutionEnabled = configuration.GetValue<bool>("BackgroundJobs:IsJobExecutionEnabled");
            options.Quartz.IsAutoRegisterEnabled = configuration.GetValue<bool>("BackgroundJobs:Quartz:IsAutoRegisterEnabled");
            options.IntakeResync.Expression = configuration.GetValue<string>("BackgroundJobs:IntakeResync:Expression") ?? "";
            options.IntakeResync.NumDaysToCheck = configuration.GetValue<string>("BackgroundJobs:IntakeResync:NumDaysToCheck") ?? "-2";
        });

        _ = context.Services.AddSingleton(provider =>
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