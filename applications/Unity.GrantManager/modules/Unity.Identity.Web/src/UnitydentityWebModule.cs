using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared.PageToolbars;
using Volo.Abp.AutoMapper;
using Volo.Abp.Http.ProxyScripting.Generators.JQuery;
using Volo.Abp.Identity.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.ObjectExtending.Modularity;
using Volo.Abp.PermissionManagement.Web;
using Volo.Abp.UI.Navigation;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.Threading;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Web;
using Unity.Identity.Web.Navigation;
using Volo.Abp.TenantManagement.Localization;
using Unity.AspNetCore.Mvc.UI.Theme.UX2;

namespace Unity.Identity.Web;

[DependsOn(typeof(AbpIdentityApplicationContractsModule))]
[DependsOn(typeof(AbpAutoMapperModule))]
[DependsOn(typeof(AbpPermissionManagementWebModule))]
[DependsOn(typeof(UnityAspNetCoreMvcUIThemeUX2Module))]
public class UnitydentityWebModule : AbpModule
{
    private static readonly OneTimeRunner OneTimeRunner = new OneTimeRunner();

    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.PreConfigure<AbpMvcDataAnnotationsLocalizationOptions>(options =>
        {
            options.AddAssemblyResource(typeof(IdentityResource), typeof(UnitydentityWebModule).Assembly);
        });

        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(UnitydentityWebModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new UnityIdentityWebMainMenuContributor());
        });

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<UnitydentityWebModule>();
        });

        context.Services.AddAutoMapperObjectMapper<UnitydentityWebModule>();

        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddProfile<UnityIdentityWebAutoMapperProfile>(validate: true);
        });

        Configure<RazorPagesOptions>(options =>
        {
            options.Conventions.AuthorizePage("/Identity/Users/Index", IdentityPermissions.Users.Default);
            options.Conventions.AuthorizePage("/Identity/Users/ImportModal", IdentityPermissions.Users.Create);
            options.Conventions.AuthorizePage("/Identity/Users/EditModal", IdentityPermissions.Users.Update);
            options.Conventions.AuthorizePage("/Identity/Roles/Index", IdentityPermissions.Roles.Default);
            options.Conventions.AuthorizePage("/Identity/Roles/CreateModal", IdentityPermissions.Roles.Create);
            options.Conventions.AuthorizePage("/Identity/Roles/EditModal", IdentityPermissions.Roles.Update);
        });

        Configure<DynamicJavaScriptProxyOptions>(options =>
        {
            options.DisableModule(IdentityRemoteServiceConsts.ModuleName);
        });

        Configure<AbpPageToolbarOptions>(options =>
        {
            options.Configure<Pages.Identity.Users.IndexModel>(
                toolbar =>
                {
                    toolbar.AddButton(
                        LocalizableString.Create<AbpTenantManagementResource>("Import"),
                        icon: " fl fl-add-to",
                        name: "ImportUser",
                        requiredPolicyName: IdentityPermissions.Users.Create,
                        type: Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Button.AbpButtonType.Light
                    );
                }
            );

            options.Configure<Pages.Identity.Roles.IndexModel>(
                toolbar =>
                {
                    toolbar.AddButton(
                        LocalizableString.Create<AbpTenantManagementResource>("NewRole"),
                        icon: " fl fl-add-to",
                        name: "CreateRole",
                        requiredPolicyName: IdentityPermissions.Roles.Create,
                        type: Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Button.AbpButtonType.Light
                    );
                }
            );
        });
    }

    public override void PostConfigureServices(ServiceConfigurationContext context)
    {
        OneTimeRunner.Run(() =>
        {
            ModuleExtensionConfigurationHelper
                .ApplyEntityConfigurationToUi(
                    IdentityModuleExtensionConsts.ModuleName,
                    IdentityModuleExtensionConsts.EntityNames.Role,
                    createFormTypes: new[] { typeof(Pages.Identity.Roles.CreateModalModel.RoleInfoModel) },
                    editFormTypes: new[] { typeof(Pages.Identity.Roles.EditModalModel.RoleInfoModel) }
                );

            ModuleExtensionConfigurationHelper
                .ApplyEntityConfigurationToUi(
                    IdentityModuleExtensionConsts.ModuleName,
                    IdentityModuleExtensionConsts.EntityNames.User,
                    createFormTypes: new[] { typeof(Pages.Identity.Users.ImportModalModel.UserImportViewModel) },
                    editFormTypes: new[] { typeof(Pages.Identity.Users.EditModalModel.UserInfoViewModel) }
                );
        });
    }
}
