using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared.PageToolbars;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Http.ProxyScripting.Generators.JQuery;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.ObjectExtending.Modularity;
using Volo.Abp.TenantManagement.Localization;
using Volo.Abp.UI.Navigation;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.Threading;
using Unity.TenantManagement.Web.Navigation;

namespace Unity.TenantManagement.Web;

[DependsOn(typeof(UnityTenantManagementApplicationContractsModule))]
[DependsOn(typeof(AbpAspNetCoreMvcUiBootstrapModule))]
[DependsOn(typeof(AbpFeatureManagementWebModule))]
[DependsOn(typeof(AbpAutoMapperModule))]
public class UnityTenantManagementWebModule : AbpModule
{
    private static readonly OneTimeRunner OneTimeRunner = new();

    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.PreConfigure<AbpMvcDataAnnotationsLocalizationOptions>(options =>
        {
            options.AddAssemblyResource(typeof(AbpTenantManagementResource), typeof(UnityTenantManagementWebModule).Assembly);
        });

        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(UnityTenantManagementWebModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new AbpTenantManagementWebMainMenuContributor());
        });

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<UnityTenantManagementWebModule>();
        });

        context.Services.AddAutoMapperObjectMapper<UnityTenantManagementWebModule>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddProfile<AbpTenantManagementWebAutoMapperProfile>(validate: true);
        });

        Configure<RazorPagesOptions>(options =>
        {
            options.Conventions.AuthorizePage("/TenantManagement/Tenants/Index", TenantManagementPermissions.Tenants.Default);
            options.Conventions.AuthorizePage("/TenantManagement/Tenants/CreateModal", TenantManagementPermissions.Tenants.Create);
            options.Conventions.AuthorizePage("/TenantManagement/Tenants/EditModal", TenantManagementPermissions.Tenants.Update);
            options.Conventions.AuthorizePage("/TenantManagement/Tenants/AssignManagerModal", TenantManagementPermissions.Tenants.Create);
        });

        Configure<AbpPageToolbarOptions>(options =>
        {
            options.Configure<Pages.TenantManagement.Tenants.IndexModel>(
                toolbar =>
                {
                    toolbar.AddButton(
                        LocalizableString.Create<AbpTenantManagementResource>("NewTenant"),
                        icon: " fl fl-add-to",
                        name: "CreateTenant",
                        requiredPolicyName: TenantManagementPermissions.Tenants.Create,
                        type: Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Button.AbpButtonType.Light
                    );
                }
            );
        });

        Configure<DynamicJavaScriptProxyOptions>(options =>
        {
            options.DisableModule(TenantManagementRemoteServiceConsts.ModuleName);
        });
    }

    public override void PostConfigureServices(ServiceConfigurationContext context)
    {
        OneTimeRunner.Run(() =>
        {
            ModuleExtensionConfigurationHelper
                .ApplyEntityConfigurationToUi(
                    TenantManagementModuleExtensionConsts.ModuleName,
                    TenantManagementModuleExtensionConsts.EntityNames.Tenant,
                    createFormTypes: new[] { typeof(Pages.TenantManagement.Tenants.CreateModalModel.TenantInfoModel) },
                    editFormTypes: new[]
                    {
                        typeof(Pages.TenantManagement.Tenants.EditModalModel.TenantInfoModel),
                        typeof(Pages.TenantManagement.Tenants.AssignManagerModalModel.AssignManagerInfoModel)
                    }
                );
        });
    }
}
