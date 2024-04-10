using Unity.Payments.BatchPaymentRequests;
using Unity.Payments.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace Unity.Payments.Settings;

public class PaymentsSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {   
       context.Add(
           new SettingDefinition(PaymentsSettings.PaymentThreshold,
            PaymentConsts.DefaultThresholdAmount.ToString("0.00"), 
            L(PaymentsSettings.Localization.PaymentThresholdDisplayName),
            L(PaymentsSettings.Localization.PaymentThresholdDescription),
            isVisibleToClients: true, 
            isInherited: false, 
            isEncrypted: false)
           .WithProviders(TenantSettingValueProvider.ProviderName)
       );
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<PaymentsResource>(name);
    }
}
