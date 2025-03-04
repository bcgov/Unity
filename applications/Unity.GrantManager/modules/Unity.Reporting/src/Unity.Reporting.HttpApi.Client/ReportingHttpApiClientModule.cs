using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Http.Client;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Unity.Reporting;

[DependsOn(
    typeof(ReportingApplicationContractsModule),
    typeof(AbpHttpClientModule))]
public class ReportingHttpApiClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClientProxies(
            typeof(ReportingApplicationContractsModule).Assembly,
            ReportingRemoteServiceConsts.RemoteServiceName
        );

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<ReportingHttpApiClientModule>();
        });

    }
}
