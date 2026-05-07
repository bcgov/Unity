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
using Unity.GrantManager.Integrations.Css;
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
using Unity.AI;
using Unity.Reporting;
using Volo.Abp.DistributedLocking;
using Unity.GrantManager.Zones;
using Unity.GrantManager.Infrastructure;
using Medallion.Threading;
using Unity.GrantManager.Locks;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;
using Unity.GrantManager.Integrations.Chefs;
using Unity.Modules.Shared.Http;
using Unity.GrantManager.Integrations.Geocoder;
using Unity.GrantManager.GrantsPortal;
using Unity.GrantManager.GrantsPortal.Configuration;
using Unity.GrantManager.GrantsPortal.Handlers;
using Unity.GrantManager.Messaging;
using Unity.GrantManager.Analytics;

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
    typeof(AIApplicationModule),
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
                        storeOptions.UseNewtonsoftJsonSerializer();
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
                    provider.ApplicantS3Folder = configuration["S3:ApplicantS3Folder"] ?? "";
                });
            });
        });

        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<GrantManagerApplicationModule>();
        });

        context.Services.AddSingleton<IAuthorizationHandler, AssessmentAuthorizationHandler>();
        context.Services.AddTransient<IResilientHttpRequest, ResilientHttpRequest>();
        context.Services.AddTransient<IFormsApiService, FormsApiService>();

        context.Services.AddScoped<IGeocoderApiService, GeocoderApiService>();    
        context.Services.AddScoped<IAnalyticsUrlProvider, MatomoUrlProvider>();
        context.Services.AddScoped<GeocoderApiService>();

        context.Services.Configure<CasClientOptions>(configuration.GetSection("Payments"));
        context.Services.Configure<CssApiOptions>(configuration.GetSection("CssApi"));
        context.Services.Configure<ChesClientOptions>(configuration.GetSection("Notifications"));

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

        // Grants Applicant Portal RabbitMQ integration
        context.Services.Configure<GrantsPortalRabbitMqOptions>(configuration.GetSection(GrantsPortalRabbitMqOptions.SectionName));
        context.Services.AddTransient<IPortalCommandHandler, ContactCreateHandler>();
        context.Services.AddTransient<IPortalCommandHandler, ContactEditHandler>();
        context.Services.AddTransient<IPortalCommandHandler, ContactSetPrimaryHandler>();
        context.Services.AddTransient<IPortalCommandHandler, ContactDeleteHandler>();
        context.Services.AddTransient<IPortalCommandHandler, AddressCreateHandler>();
        context.Services.AddTransient<IPortalCommandHandler, AddressEditHandler>();
        context.Services.AddTransient<IPortalCommandHandler, AddressSetPrimaryHandler>();
        context.Services.AddTransient<IPortalCommandHandler, AddressDeleteHandler>();
        context.Services.AddTransient<IPortalCommandHandler, OrganizationEditHandler>();

        // Register generic IInboxMessageHandler adapters for each portal command handler
        context.Services.AddTransient<IInboxMessageHandler>(sp => new PortalCommandHandlerAdapter(sp.GetRequiredService<ContactCreateHandler>()));
        context.Services.AddTransient<IInboxMessageHandler>(sp => new PortalCommandHandlerAdapter(sp.GetRequiredService<ContactEditHandler>()));
        context.Services.AddTransient<IInboxMessageHandler>(sp => new PortalCommandHandlerAdapter(sp.GetRequiredService<ContactSetPrimaryHandler>()));
        context.Services.AddTransient<IInboxMessageHandler>(sp => new PortalCommandHandlerAdapter(sp.GetRequiredService<ContactDeleteHandler>()));
        context.Services.AddTransient<IInboxMessageHandler>(sp => new PortalCommandHandlerAdapter(sp.GetRequiredService<AddressCreateHandler>()));
        context.Services.AddTransient<IInboxMessageHandler>(sp => new PortalCommandHandlerAdapter(sp.GetRequiredService<AddressEditHandler>()));
        context.Services.AddTransient<IInboxMessageHandler>(sp => new PortalCommandHandlerAdapter(sp.GetRequiredService<AddressSetPrimaryHandler>()));
        context.Services.AddTransient<IInboxMessageHandler>(sp => new PortalCommandHandlerAdapter(sp.GetRequiredService<AddressDeleteHandler>()));
        context.Services.AddTransient<IInboxMessageHandler>(sp => new PortalCommandHandlerAdapter(sp.GetRequiredService<OrganizationEditHandler>()));

        context.Services.AddScoped<GrantsPortalAcknowledgmentPublisher>();
        context.Services.AddHostedService<GrantsPortalCommandConsumerService>();  // RabbitMQ → inbox table

        context.Services.AddScoped<IZoneChecker, ZoneChecker>();

        context.Services.AddSingleton(provider =>
        {
            var options = (provider.GetService<IOptions<IntakeClientOptions>>()?.Value) ?? throw new InvalidOperationException("IntakeClientOptions not configured.");
            if (options.BaseUri == string.Empty)
            {
                options.BaseUri = "https://submit.digital.gov.bc.ca/app/api/v1";
            }

            var restOptions = options != null
                ? new RestClientOptions(options.BaseUri)
                {
                    FailOnDeserializationError = true,
                    ThrowOnDeserializationError = true
                }
                : new RestClientOptions();

            return new RestClient(
                restOptions,
                configureSerialization: s => s.UseSystemTextJson(new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                })
            );
        });

        // Max paging limits
        ExtensibleLimitedResultRequestDto.DefaultMaxResultCount = int.MaxValue;
        ExtensibleLimitedResultRequestDto.MaxMaxResultCount = int.MaxValue;
        LimitedResultRequestDto.DefaultMaxResultCount = int.MaxValue;
        LimitedResultRequestDto.MaxMaxResultCount = int.MaxValue;
    }

    private void ConfigureBackgroundServices(IConfiguration configuration)
    {
        if (!Convert.ToBoolean(configuration["BackgroundJobs:IsJobExecutionEnabled"]))
            return;

        Configure<AbpBackgroundJobOptions>(options =>
        {
            options.IsJobExecutionEnabled = configuration.GetValue<bool>("BackgroundJobs:IsJobExecutionEnabled");
        });

        Configure<AbpBackgroundWorkerQuartzOptions>(options =>
        {
            options.IsAutoRegisterEnabled = configuration.GetValue<bool>("BackgroundJobs:Quartz:IsAutoRegisterEnabled");
        });

        Configure<BackgroundJobsOptions>(options =>
        {
            options.IsJobExecutionEnabled = configuration.GetValue<bool>("BackgroundJobs:IsJobExecutionEnabled");
            options.Quartz.IsAutoRegisterEnabled = configuration.GetValue<bool>("BackgroundJobs:Quartz:IsAutoRegisterEnabled");
        });
    }
}
