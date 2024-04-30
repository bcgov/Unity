using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using Volo.Abp.Domain;
using Volo.Abp.Validation;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Localization;
using Volo.Abp.Validation.Localization;
using Volo.Abp.VirtualFileSystem;
using Localization.Resources.AbpUi;
using Unity.Flex.EntityFrameworkCore;
using Unity.Flex.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex;

[DependsOn(
    typeof(FlexApplicationContractsModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpValidationModule),
    typeof(AbpDddDomainSharedModule)
    )]
public class FlexApplicationModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(FlexApplicationModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<FlexApplicationModule>();
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Add<FlexResource>("en")
                .AddBaseTypes(typeof(AbpValidationResource))
                .AddVirtualJson("/Localization/Flex");
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<FlexResource>()
                .AddBaseTypes(typeof(AbpUiResource));
        });

        Configure<AbpExceptionLocalizationOptions>(options =>
        {
            options.MapCodeNamespace("Flex", typeof(FlexResource));
        });

        context.Services.AddAutoMapperObjectMapper<FlexApplicationModule>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<FlexApplicationModule>(validate: true);
        });

        context.Services.AddHttpClientProxies(
           typeof(FlexApplicationContractsModule).Assembly,
           FlexRemoteServiceConsts.RemoteServiceName
        );

        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(FlexApplicationModule).Assembly);
        });

        context.Services.AddAssemblyOf<FlexApplicationModule>();

        context.Services.AddAbpDbContext<FlexDbContext>(options =>
#pragma warning disable S125 // Sections of code should not be commented out
        {
            /* Add custom repositories here. Example:
             * options.AddRepository<Question, EfCoreQuestionRepository>();
             */
        }
#pragma warning restore S125 // Sections of code should not be commented out
);
    }
}
