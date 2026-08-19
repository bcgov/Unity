using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Unity.Notifications.Localization;
using Unity.Notifications.Web.Menus;
using Unity.Notifications.Web.Realtime;
using Unity.Notifications.Web.Views.Settings;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.Mapperly;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.Modularity;
using Volo.Abp.SettingManagement.Web;
using Volo.Abp.SettingManagement.Web.Pages.SettingManagement;
using Volo.Abp.UI.Navigation;
using Volo.Abp.VirtualFileSystem;

namespace Unity.Notifications.Web;

[DependsOn(
    typeof(NotificationsApplicationModule),
    typeof(NotificationsApplicationContractsModule),
    typeof(AbpAspNetCoreMvcUiThemeSharedModule),
    typeof(AbpAspNetCoreSignalRModule),
    typeof(AbpMapperlyModule),
    typeof(AbpSettingManagementWebModule)
    )]
public class NotificationsWebModule : AbpModule
{
    private const string RedisPasswordKey = "Redis:Password";
    private const string RedisHostKey = "Redis:Host";
    private const string RedisPortKey = "Redis:Port";
    private const string RedisDefaultDatabaseKey = "Redis:DatabaseId";
    private const string RedisConfigurationKey = "Redis:Configuration";
    private const string RedisSentinelMasterNameKey = "Redis:SentinelMasterName";

    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.PreConfigure<AbpMvcDataAnnotationsLocalizationOptions>(options =>
        {
            options.AddAssemblyResource(typeof(NotificationsResource), typeof(NotificationsWebModule).Assembly);
        });

        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(NotificationsApplicationModule).Assembly);
        });

        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(NotificationsWebModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        var signalRBuilder = context.Services
            .AddSignalR()
            .AddHubOptions<NotificationHub>(options =>
            {
                options.EnableDetailedErrors = false;
            });

        ConfigureSignalRRedisBackplane(signalRBuilder, configuration);

        context.Services.AddSingleton<INotificationPresenceTracker, NotificationPresenceTracker>();
        context.Services.AddTransient<NotificationLogCreatedRealtimeHandler>();

        Configure<AbpBackgroundJobOptions>(options =>
        {
            options.IsJobExecutionEnabled = configuration.GetValue<bool>("BackgroundJobs:IsJobExecutionEnabled");
        });

        Configure<AbpBackgroundWorkerQuartzOptions>(options =>
        {
            options.IsAutoRegisterEnabled = configuration.GetValue<bool>("BackgroundJobs:Quartz:IsAutoRegisterEnabled");
        });

        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new NotificationsMenuContributor());
            options.MenuContributors.Add(new NotificationLogsUserMenuContributor());
        });

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<NotificationsWebModule>();
        });

        Configure<SettingManagementPageOptions>(options =>
        {
            options.Contributors.Add(new NotificationsSettingPageContributor());
        });

        context.Services.AddMapperlyObjectMapper<NotificationsWebModule>();

        Configure<RazorPagesOptions>(options =>
        {
            //Configure authorization.
        });
    }

    private static void ConfigureSignalRRedisBackplane(ISignalRServerBuilder signalRBuilder, IConfiguration configuration)
    {
        if (!Convert.ToBoolean(configuration["Redis:IsEnabled"]))
        {
            return;
        }

        var useSentinel = Convert.ToBoolean(configuration["Redis:UseSentinel"]);
        var channelPrefix = configuration["Notifications:SignalR:ChannelPrefix"] ?? "unity:signalr";

        if (useSentinel)
        {
            signalRBuilder.AddStackExchangeRedis(options =>
            {
                options.Configuration = BuildSentinelOptions(configuration);
                options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal(channelPrefix);
            });

            return;
        }

        var connectionString = BuildStandardConnectionString(configuration);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        signalRBuilder.AddStackExchangeRedis(connectionString, options =>
        {
            options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal(channelPrefix);
        });
    }

    private static StackExchange.Redis.ConfigurationOptions BuildSentinelOptions(IConfiguration configuration)
    {
        var options = new StackExchange.Redis.ConfigurationOptions
        {
            ServiceName = configuration[RedisSentinelMasterNameKey],
            Password = configuration[RedisPasswordKey],
            AbortOnConnectFail = false,
            AllowAdmin = true,
            DefaultVersion = new Version(7, 0, 0),
            DefaultDatabase = configuration.GetValue<int>(RedisDefaultDatabaseKey, 0)
        };

        var endpoints = configuration[RedisConfigurationKey]?.Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (endpoints != null)
        {
            foreach (var endpoint in endpoints)
            {
                options.EndPoints.Add(endpoint.Trim());
            }
        }

        return options;
    }

    private static string BuildStandardConnectionString(IConfiguration configuration)
    {
        var host = configuration[RedisHostKey];
        var port = configuration[RedisPortKey];
        var password = configuration[RedisPasswordKey];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(port))
        {
            return string.Empty;
        }

        var passwordSegment = string.IsNullOrWhiteSpace(password) ? string.Empty : $",password={password}";
        return $"{host}:{port}{passwordSegment},abortConnect=false";
    }
}
