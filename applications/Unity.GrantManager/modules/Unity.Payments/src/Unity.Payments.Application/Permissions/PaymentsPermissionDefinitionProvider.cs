using Unity.Modules.Shared;
using Unity.Payments.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;
using Volo.Abp.Localization;

namespace Unity.Payments.Permissions;

public class PaymentsPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var grantApplicationPermissionsGroup = context.AddGroup(PaymentsPermissions.GroupName, L("Permission:Payments"));

        // Payment Requests
        var paymentsPermissions = grantApplicationPermissionsGroup.AddPermission(PaymentsPermissions.Payments.Default, L("Permission:Payments.Default"));
        paymentsPermissions.AddChild(PaymentsPermissions.Payments.L1ApproveOrDecline, L("Permission:Payments.L1ApproveOrDecline"));
        paymentsPermissions.AddChild(PaymentsPermissions.Payments.L2ApproveOrDecline, L("Permission:Payments.L2ApproveOrDecline"));
        paymentsPermissions.AddChild(PaymentsPermissions.Payments.L3ApproveOrDecline, L("Permission:Payments.L3ApproveOrDecline"));
        paymentsPermissions.AddChild(PaymentsPermissions.Payments.RequestPayment, L("Permission:Payments.RequestPayment"));

        //-- PAYMENT INFO PERMISSIONS
        grantApplicationPermissionsGroup.Add_PaymentInfo_Permissions();
        paymentsPermissions.AddChild(PaymentsPermissions.Payments.EditSupplierInfo, L("Permission:Payments.EditSupplierInfo"));
        paymentsPermissions.AddChild(PaymentsPermissions.Payments.EditFormPaymentConfiguration, L("Permission:Payments.EditFormPaymentConfiguration"));        
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<PaymentsResource>(name);
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1481:Unused local variables should be removed", Justification = "Configuration Code")]
public static class PaymentPermissionGroupDefinitionExtensions
{
    public static void Add_PaymentInfo_Permissions(this PermissionGroupDefinition grantApplicationPermissionsGroup)
    {
        #region PAYMENT INFO GRANULAR PERMISSIONS
        var upx_Payment                                     = grantApplicationPermissionsGroup
                                                                                    .AddPermission(UnitySelector.Payment.Default, LocalizableString.Create<PaymentsResource>(UnitySelector.Payment.Default))
                                                                                    .RequireFeatures("Unity.Payments");

        var upx_Payment_Summary                             = upx_Payment.AddPaymentChild(UnitySelector.Payment.Summary.Default);

        var upx_Payment_Supplier                            = upx_Payment.AddPaymentChild(UnitySelector.Payment.Supplier.Default);
        var upx_Payment_Supplier_Update                     = upx_Payment_Supplier.AddPaymentChild(UnitySelector.Payment.Supplier.Update);

        var upx_PaymentList_Authority                       = upx_Payment.AddPaymentChild(UnitySelector.Payment.PaymentList.Default);
        #endregion
    }

    

    public static PermissionDefinition AddPaymentChild(this PermissionDefinition parent, string name)
    {
        return parent.AddChild(name, LocalizableString.Create<PaymentsResource>(name)).RequireFeatures("Unity.Payments");
    }
}
