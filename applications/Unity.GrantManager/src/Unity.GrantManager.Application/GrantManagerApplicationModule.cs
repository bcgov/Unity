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
using Volo.Abp.Quartz;
using System;
using Quartz;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ;
using Volo.Abp.BackgroundJobs;
using Unity.Reporting;
using Volo.Abp.DistributedLocking;
using Unity.GrantManager.Zones;
using Unity.GrantManager.Infrastructure;
using Medallion.Threading;
using Unity.GrantManager.Locks;

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
    typeof(NotificationsApplicationModule),
    typeof(PaymentsApplicationModule),
    typeof(FlexApplicationModule),
    typeof(ReportingApplicationModule),
    typeof(AbpDistributedLockingModule)
)]
public class GrantManagerApplicationModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        PreConfigure<AbpQuartzOptions>(options =>
        {
            options.Configurator = configure =>
            {
                configure.SchedulerName = Guid.NewGuid().ToString();
            };
        });

        if (Convert.ToBoolean(configuration["BackgroundJobs:Quartz:UseCluster"]))
        {
            PreConfigure<AbpQuartzOptions>(options =>
            {
                options.Configurator = configure =>
                {
                    configure.UsePersistentStore(storeOptions =>
                    {
                        storeOptions.UseProperties = true;
                        storeOptions.UsePostgres(configuration.GetConnectionString("Default") ?? string.Empty);
                        storeOptions.UseClustering(t =>
                        {
                            t.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
                            t.CheckinInterval = TimeSpan.FromSeconds(10);
                        });
                        storeOptions.UseJsonSerializer();
                        storeOptions.SetProperty("quartz.jobStore.tablePrefix", "qrtz_");
                        storeOptions.SetProperty("quartz.scheduler.instanceName", "UnityQuartz");
                        storeOptions.SetProperty("quartz.scheduler.instanceId", "AUTO");
                    });
                };
            });
        }
    }

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

        ConfigureBackgroundServices(configuration);

        if (Convert.ToBoolean(configuration["Redis:IsEnabled"]))
        {
            RedisInfrastructureManager.ConfigureRedis(context.Services, configuration);
        }
        else
        {
            // We need this because adding the AbpDistributedLockingModule requires a provider
            context.Services.AddSingleton<IDistributedLockProvider, InMemoryDistributedLockProvider>();
        }

        context.Services.ConfigureRabbitMQ();
        context.Services.AddScoped<IZoneChecker, ZoneChecker>();

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
        ExtensibleLimitedResultRequestDto.DefaultMaxResultCount = int.MaxValue;
        ExtensibleLimitedResultRequestDto.MaxMaxResultCount = int.MaxValue;

        LimitedResultRequestDto.DefaultMaxResultCount = int.MaxValue;
        LimitedResultRequestDto.MaxMaxResultCount = int.MaxValue;
    }

    private void ConfigureBackgroundServices(IConfiguration configuration)
    {
        if (!Convert.ToBoolean(configuration["BackgroundJobs:IsJobExecutionEnabled"])) return;

        Configure<AbpBackgroundJobOptions>(options =>
        {
            options.IsJobExecutionEnabled = configuration.GetValue<bool>("BackgroundJobs:IsJobExecutionEnabled");
        });

        Configure<AbpBackgroundWorkerQuartzOptions>(options =>
        {
            options.IsAutoRegisterEnabled = configuration.GetValue<bool>("BackgroundJobs:Quartz:IsAutoRegisterEnabled");
        });

        /*
         * There are Global Retry Options that can be configured, configure if required, or if handled per job 
        */
        Configure<BackgroundJobsOptions>(options =>
        {
            options.IsJobExecutionEnabled = configuration.GetValue<bool>("BackgroundJobs:IsJobExecutionEnabled");
            options.Quartz.IsAutoRegisterEnabled = configuration.GetValue<bool>("BackgroundJobs:Quartz:IsAutoRegisterEnabled");
        });
    }
}