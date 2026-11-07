using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.Web.Filters;
using Unity.GrantManager.Web.Menus;
using Unity.GrantManager.Web.Pages.Account;
using Unity.Modules.Shared;
using Unity.Modules.Shared.Permissions;
using Unity.Payments.Permissions;
using Unity.Payments.Web.Menus;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;
using Volo.Abp.UI.Navigation;
using Volo.Abp.Users;
using Xunit;

namespace Unity.GrantManager.Pages;

public class PageAccessTests
{
    private readonly IMenuManager _menus = Substitute.For<IMenuManager>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ApplicationMenu _menu = new(StandardMenus.Main);

    public PageAccessTests()
    {
        _user.IsAuthenticated.Returns(true);
        _menus.GetMainMenuAsync().Returns(_menu);
    }

    private UserNavigationService CreateNavigation() => new(_menus, _user, _permissions);

    [Fact]
    public async Task Login_WithoutAccessibleAreas_ShouldRedirectToExistingNoAccessPage()
    {
        var page = new LoginModel(CreateNavigation());

        var result = (await page.OnGetAsync()).ShouldBeOfType<LocalRedirectResult>();

        result.Url.ShouldBe(UserNavigationService.NoAccessUrl);
    }

    [Fact]
    public async Task Login_WithARoleButNoAccessibleAreas_ShouldStillDenyAccess()
    {
        _user.IsInRole("reviewer").Returns(true);

        (await CreateNavigation().GetLandingPageAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task Login_WithOnlyPaymentAccess_ShouldOpenPaymentsWithoutRequiringARole()
    {
        _menu.AddItem(new ApplicationMenuItem(PaymentsMenus.Prefix, "Payments", "~/PaymentRequests"));

        var result = (await new LoginModel(CreateNavigation()).OnGetAsync())
            .ShouldBeOfType<LocalRedirectResult>();

        result.Url.ShouldBe("/PaymentRequests");
    }

    [Fact]
    public async Task Login_ShouldChooseFirstPermittedAreaInMenuOrder()
    {
        _menu.AddItem(new ApplicationMenuItem(PaymentsMenus.Prefix, "Payments", "~/PaymentRequests"));
        _menu.AddItem(new ApplicationMenuItem(GrantManagerMenus.Applications, "Applications", "~/GrantApplications", order: 1));

        (await CreateNavigation().GetLandingPageAsync()).ShouldBe("/GrantApplications");
    }

    [Fact]
    public async Task Login_ShouldIgnoreHomeExternalAndDisabledLinks()
    {
        _menu.AddItem(new ApplicationMenuItem(GrantManagerMenus.Home, "Home", "~/"));
        _menu.AddItem(new ApplicationMenuItem("External", "External", "https://example.com"));
        _menu.AddItem(new ApplicationMenuItem("Disabled", "Disabled", "~/Disabled") { IsDisabled = true });

        (await CreateNavigation().GetLandingPageAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task Login_WithOnlyConfigurationAccess_ShouldOpenConfiguration()
    {
        _permissions.IsGrantedAsync(UnitySettingManagementPermissions.UserInterface).Returns(true);

        (await CreateNavigation().GetLandingPageAsync()).ShouldBe("/ConfigurationManagement");
    }

    [Fact]
    public async Task Login_ForHostAdministrator_ShouldKeepTenantManagementDestination()
    {
        _user.IsInRole(IdentityConsts.ITAdminRoleName).Returns(true);

        var navigation = CreateNavigation();

        (await navigation.GetLandingPageAsync()).ShouldBe("/TenantManagement/Tenants");
        (await navigation.CanAccessPageAsync("/GrantApplications/Index")).ShouldBeTrue();
    }

    [Fact]
    public async Task Login_ForOperationsOnOnboardingTenant_ShouldKeepOnboardingDestination()
    {
        _user.IsInRole(IdentityConsts.ITOperationsRoleName).Returns(true);
        _menu.AddItem(new ApplicationMenuItem("Onboarding", "Onboarding", "~/TenantManagement/Onboarding", order: 1));

        (await CreateNavigation().GetLandingPageAsync()).ShouldBe("/TenantManagement/Onboarding");
    }

    [Theory]
    [InlineData("/GrantApplications/Index")]
    [InlineData("/GrantApplications/Details")]
    [InlineData("/PaymentRequests/Index")]
    [InlineData("/PaymentRequests/CreatePaymentRequests")]
    [InlineData("/Index")]
    public async Task Page_WithoutAccessibleAreas_ShouldRedirectBeforeRunningHandler(string pagePath)
    {
        var (context, called) = await ExecuteFilterAsync(pagePath);

        context.Result.ShouldBeOfType<LocalRedirectResult>().Url.ShouldBe(UserNavigationService.NoAccessUrl);
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task Page_WithApplicationAccessButNoPaymentAccess_ShouldDenyDirectPaymentUrl()
    {
        _menu.AddItem(new ApplicationMenuItem(GrantManagerMenus.Applications, "Applications", "~/GrantApplications"));

        var (context, called) = await ExecuteFilterAsync("/PaymentRequests/Index");

        context.Result.ShouldBeOfType<LocalRedirectResult>().Url.ShouldBe(UserNavigationService.NoAccessUrl);
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task Page_WithPaymentAccessButNoApplicationAccess_ShouldDenyDirectApplicationUrl()
    {
        _menu.AddItem(new ApplicationMenuItem(PaymentsMenus.Prefix, "Payments", "~/PaymentRequests"));

        var (context, called) = await ExecuteFilterAsync("/GrantApplications/Details");

        context.Result.ShouldBeOfType<LocalRedirectResult>().Url.ShouldBe(UserNavigationService.NoAccessUrl);
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task Page_WithPaymentAccess_ShouldRunPaymentHandler()
    {
        _menu.AddItem(new ApplicationMenuItem(PaymentsMenus.Prefix, "Payments", "~/PaymentRequests"));

        var (context, called) = await ExecuteFilterAsync("/PaymentRequests/Index");

        context.Result.ShouldBeNull();
        called.ShouldBeTrue();
    }

    [Theory]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Logout")]
    [InlineData("/Account/NoGrantPrograms")]
    [InlineData("/Account/AccessDenied")]
    [InlineData("/GrantPrograms/Index")]
    [InlineData("/Error")]
    public async Task RecoveryPages_ShouldRemainAccessibleWithoutPermissions(string pagePath)
    {
        var (context, called) = await ExecuteFilterAsync(pagePath);

        context.Result.ShouldBeNull();
        called.ShouldBeTrue();
    }

    [Fact]
    public async Task AnonymousHome_ShouldRetainExistingBehavior()
    {
        _user.IsAuthenticated.Returns(false);

        var (context, called) = await ExecuteFilterAsync("/Index");

        context.Result.ShouldBeNull();
        called.ShouldBeTrue();
    }

    [Fact]
    public async Task NewRequest_ShouldReevaluateAccessAfterTenantOrPermissionChange()
    {
        _menu.AddItem(new ApplicationMenuItem(GrantManagerMenus.Applications, "Applications", "~/GrantApplications"));
        _menus.GetMainMenuAsync().Returns(_menu, new ApplicationMenu(StandardMenus.Main));

        (await CreateNavigation().GetLandingPageAsync()).ShouldBe("/GrantApplications");
        (await CreateNavigation().GetLandingPageAsync()).ShouldBeNull();
    }

    [Theory]
    [InlineData(true, null, false)]
    [InlineData(true, PaymentsPermissions.Payments.Default, true)]
    [InlineData(true, UnitySelector.Payment.PaymentList.Default, true)]
    [InlineData(false, PaymentsPermissions.Payments.Default, false)]
    public async Task PaymentsMenu_ShouldRequireFeatureAndExistingViewingPermission(
        bool featureEnabled, string? permission, bool visible)
    {
        var features = Substitute.For<IFeatureChecker>();
        features.IsEnabledAsync("Unity.Payments").Returns(featureEnabled);
        if (permission != null)
        {
            _permissions.IsGrantedAsync(permission).Returns(true);
        }

        using var services = new ServiceCollection()
            .AddTransient<IAbpLazyServiceProvider, AbpLazyServiceProvider>()
            .AddSingleton(features)
            .AddSingleton(_permissions)
            .BuildServiceProvider();
        var context = new MenuConfigurationContext(_menu, services);

        await new PaymentsMenuContributor().ConfigureMenuAsync(context);

        _menu.Items.Count.ShouldBe(visible ? 1 : 0);
    }

    private async Task<(PageHandlerExecutingContext Context, bool HandlerCalled)> ExecuteFilterAsync(string pagePath)
    {
        var pageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData(),
            ActionDescriptor = new CompiledPageActionDescriptor { ViewEnginePath = pagePath }
        };
        var handler = new object();
        var context = new PageHandlerExecutingContext(pageContext, [], null, new Dictionary<string, object?>(), handler);
        var called = false;

        await new PageAccessFilter(CreateNavigation(), _user).OnPageHandlerExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult(new PageHandlerExecutedContext(pageContext, [], null, handler));
        });

        return (context, called);
    }
}
