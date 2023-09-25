using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace Unity.GrantManager.HttpApi.Client.ConsoleTestApp;

public static class Program
{
    static async Task Main(string[] args)
    {
        await CreateHostBuilder(args).RunConsoleAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .AddAppSettingsSecretsJson()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<ConsoleTestAppHostedService>();
            });
}
