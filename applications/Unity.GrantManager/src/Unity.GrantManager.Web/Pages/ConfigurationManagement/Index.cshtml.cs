using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Unity.Payments.PaymentConfigurations;

namespace Unity.GrantManager.Web.Pages.ConfigurationManagement;

[Authorize(UnitySettingManagementPermissions.UserInterface)]
public class IndexModel(
    IPaymentConfigurationAppService paymentConfigurationAppService,
    IConfiguration configuration) : GrantManagerPageModel
{
    public Guid? AccountCodingId { get; set; }
    public string? PaymentIdPrefix { get; set; }
    public string MaxFileSize { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        MaxFileSize = configuration["S3:MaxFileSize"] ?? "";

        var paymentConfiguration = await paymentConfigurationAppService.GetAsync();
        if (paymentConfiguration != null)
        {
            AccountCodingId = paymentConfiguration.DefaultAccountCodingId;
            PaymentIdPrefix = paymentConfiguration.PaymentIdPrefix;
        }
    }
}
