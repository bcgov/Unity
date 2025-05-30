using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Volo.Abp.Caching;
using System;
using Medallion.Threading.Redis;
using Medallion.Threading;
using System.Threading.Tasks;

namespace Unity.GrantManager.Infrastructure
{
    internal static class RedisInfrastructureManager
    {
        private const string RedisInstanceNameKey = "Redis:InstanceName";
        private const string RedisPasswordKey = "Redis:Password";
        private const string RedisHostKey = "Redis:Host";
        private const string RedisPortKey = "Redis:Port";
        private const string RedisDefaultDatabaseKey = "Redis:DatabaseId";
        private const string RedisConfigurationKey = "Redis:Configuration";
        private const string RedisSentinelMasterNameKey = "Redis:SentinelMasterName";
        private const string RedisUseSentinel = "Redis:UseSentinel";
        private const string RedisKeyPrefix = "Redis:KeyPrefix";

        internal static void ConfigureRedis(IServiceCollection services, IConfiguration configuration)
        {
            var useSentinel = Convert.ToBoolean(configuration[RedisUseSentinel]);

            // Helper to build ConfigurationOptions for Sentinel
            ConfigurationOptions BuildSentinelOptions()
            {
                var sentinelMasterName = configuration[RedisSentinelMasterNameKey];
                var endpointConfiguration = configuration[RedisConfigurationKey]?.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var password = configuration[RedisPasswordKey];
                var defaultDatabase = configuration.GetValue<int>(RedisDefaultDatabaseKey, 0);

                var options = new ConfigurationOptions
                {
                    ServiceName = sentinelMasterName,
                    Password = password,
                    AbortOnConnectFail = false,
                    AllowAdmin = true,
                    DefaultVersion = new Version(7, 0, 0),
                    DefaultDatabase = defaultDatabase,
                };

                if (endpointConfiguration != null)
                {
                    foreach (var endpoint in endpointConfiguration)
                    {
                        options.EndPoints.Add(endpoint.Trim());
                    }
                }

                return options;
            }

            // Helper to build standard connection string
            string BuildConnectionString() =>
                $"{configuration[RedisHostKey]}:{configuration[RedisPortKey]},password={configuration[RedisPasswordKey]},abortConnect=false";

            // Lazy multiplexer registration
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                if (useSentinel)
                {
                    var options = BuildSentinelOptions();
                    return ConnectionMultiplexer.Connect(options);
                }
                else
                {
                    var connStr = BuildConnectionString();
                    return ConnectionMultiplexer.Connect(connStr);
                }
            });

            // Configure the cache
            services.AddStackExchangeRedisCache(options =>
            {
                if (useSentinel)
                {
                    var sentinelOptions = BuildSentinelOptions();
                    options.ConfigurationOptions = sentinelOptions;
                    options.ConnectionMultiplexerFactory = () =>
                    {
                        // Use the same multiplexer as registered in DI
                        var provider = services.BuildServiceProvider();
                        var muxer = provider.GetRequiredService<IConnectionMultiplexer>();
                        return Task.FromResult(muxer);
                    };
                }
                else
                {
                    options.InstanceName = configuration[RedisInstanceNameKey];
                    options.Configuration = BuildConnectionString();
                }
            });

            services.Configure<AbpDistributedCacheOptions>(options =>
            {
                options.KeyPrefix = configuration[RedisKeyPrefix] ?? "unity";
            });

            // Distributed lock provider
            services.AddSingleton<IDistributedLockProvider>(sp =>
            {
                var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
                return new RedisDistributedSynchronizationProvider(multiplexer.GetDatabase());
            });
        }
    }
}
