using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Unity.GrantManager.Web;

public static class Program
{
    // Test Sonar Qube run
    public async static Task<int> Main(string[] args)
    {
        try
        {
            Log.Information("Starting web host.");
            // Using this for now as logger not registered yet
            Console.WriteLine("Starting web host.");

            var builder = WebApplication.CreateBuilder(args);


            // ✅ Correct for your hosting model
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestHeadersTotalSize = 32768;
            });
            builder.Services.AddHttpContextAccessor();

            builder.Host.AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog((hostingContext, loggerConfiguration) =>
                    loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration));

            await builder.AddApplicationAsync<GrantManagerWebModule>();

            var app = builder.Build();

            app.MapHealthChecks("/healthz/live",
                new HealthCheckOptions() { Predicate = healthCheck => healthCheck.Tags.Contains("live") });

            app.MapHealthChecks("/healthz/ready",
                new HealthCheckOptions() { Predicate = healthCheck => healthCheck.Tags.Contains("ready") });

            app.MapHealthChecks("/healthz/startup",
                new HealthCheckOptions() { Predicate = healthCheck => healthCheck.Tags.Contains("startup") });

            /*
                Liveness probe. This is for detecting whether the application process has crashed/deadlocked. If a liveness probe fails, Kubernetes will stop the pod, and create a new one.
                Readiness probe. This is for detecting whether the application is ready to handle requests. If a readiness probe fails, Kubernetes will leave the pod running, but won't send any requests to the pod.
                Startup probe. This is used when the container starts up, to indicate that it's ready. Once the startup probe succeeds, Kubernetes switches to using the liveness probe to determine if the application is alive. This probe was introduced in Kubernetes version 1.16.
            */

            await app.InitializeApplicationAsync();
            await app.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "Host terminated unexpectedly!");
            // Using this for now as logger not registered yet
            Console.WriteLine(ex);

            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}