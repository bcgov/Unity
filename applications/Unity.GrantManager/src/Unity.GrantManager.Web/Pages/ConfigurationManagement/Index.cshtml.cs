using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Unity.AI.Permissions;
using Unity.GrantManager.Permissions;
using Unity.Modules.Shared;
using Unity.Notifications.Permissions;
using Unity.Payments.PaymentConfigurations;
using Unity.Payments.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;

namespace Unity.GrantManager.Web.Pages.ConfigurationManagement;

[Authorize(UnitySettingManagementPermissions.UserInterface)]
public class IndexModel(
    IPaymentConfigurationAppService paymentConfigurationAppService,
    IConfiguration configuration,
    IFeatureChecker featureChecker,
    IPermissionChecker permissionChecker) : GrantManagerPageModel
{
    public Guid? AccountCodingId { get; set; }
    public string? PaymentIdPrefix { get; set; }
    public string MaxFileSize { get; set; } = string.Empty;

    // Visibility flags
    public bool ShowNotifications { get; set; }
    public bool ShowPayments { get; set; }
    public bool ShowPaymentAccountCoding { get; set; }
    public bool ShowPaymentSettings { get; set; }
    public bool ShowCustomFields { get; set; }
    public bool ShowScoresheets { get; set; }
    public bool ShowTags { get; set; }
    public bool ShowAI { get; set; }

    public async Task OnGetAsync()
    {
        MaxFileSize = configuration["S3:MaxFileSize"] ?? "";

        // Resolve feature + permission flags
        ShowNotifications = await featureChecker.IsEnabledAsync("Unity.Notifications")
            && await permissionChecker.IsGrantedAsync(NotificationsPermissions.Settings);

        bool isPaymentsFeatureEnabled = await featureChecker.IsEnabledAsync("Unity.Payments");
        bool isAuthorizedForPaymentConfiguration = await permissionChecker.IsGrantedAsync(UnitySettingManagementPermissions.ConfigurePayments);
        ShowPayments = isPaymentsFeatureEnabled && isAuthorizedForPaymentConfiguration;

        ShowPaymentAccountCoding = ShowPayments
            && await permissionChecker.IsGrantedAsync(PaymentsPermissions.Payments.Default);
        ShowPaymentSettings = ShowPayments
            && await permissionChecker.IsGrantedAsync(UnitySelector.Payment.Summary.Default);

        ShowCustomFields = await featureChecker.IsEnabledAsync("Unity.Flex");
        ShowScoresheets = await featureChecker.IsEnabledAsync("Unity.Flex");

        ShowTags = await permissionChecker.IsGrantedAsync(UnitySelector.SettingManagement.Tags.Default);

        ShowAI = await featureChecker.IsEnabledAsync("Unity.AI.Scoring")
            && await permissionChecker.IsGrantedAsync(AIPermissions.Configuration.ConfigureAI);

        // Load payment data only if visible
        if (ShowPayments)
        {
            var paymentConfiguration = await paymentConfigurationAppService.GetAsync();
            if (paymentConfiguration != null)
            {
                AccountCodingId = paymentConfiguration.DefaultAccountCodingId;
                PaymentIdPrefix = paymentConfiguration.PaymentIdPrefix;
            }
        }
    }
}
