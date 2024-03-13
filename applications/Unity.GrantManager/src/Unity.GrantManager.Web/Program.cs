using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.HealthChecks;

namespace Unity.GrantManager.Web;

public static class Program
{
    public async static Task<int> Main(string[] args)
    {
        try
        {            
            Log.Information("Starting web host.");
            // Using this for now as logger not registered yet
            Console.WriteLine("Starting web host.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddHttpContextAccessor();
            builder.Host.AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog((hostingContext, loggerConfiguration) =>
                loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration));
            await builder.AddApplicationAsync<GrantManagerWebModule>();
            builder.Services.AddSingleton<UnityHealthChecks>();
            builder.Services.AddHealthChecks().AddCheck<UnityHealthChecks>("UnityHealthCheck");
            var app = builder.Build();
            app.MapHealthChecks("/healthz");
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
            Log.CloseAndFlush();
        }
    }
}