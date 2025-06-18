using Unity.GrantManager.Localization;
using Unity.Modules.Shared;
using Unity.Payments.Localization;
using Volo.Abp.Authorization.Permissions;
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
        
        // NOTE: Review location in hierarchy
        paymentsPermissions.AddChild(PaymentsPermissions.Payments.EditSupplierInfo, L("Permission:Payments.EditSupplierInfo"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<PaymentsResource>(name);
    }
}

public static class PaymentsPermissionGroupDefinitionExtensions
{
    // NOTE: Will be included as part of AB#28018
    public static void AddApplication_PaymentInfo_Permissions(this PermissionGroupDefinition permissionGroup)
    {
        #region PAYMENT INFO PERMISSIONS
        var upx_Payment                     = permissionGroup.AddPermission(UnitySelector.Applicant.Default, L(UnitySelector.Applicant.Default));

        var upx_Payment_Summary             = upx_Payment.AddUnityChild(UnitySelector.Payment.Summary.Default);

        var upx_Payment_Supplier            = upx_Payment.AddUnityChild(UnitySelector.Payment.Supplier.Default);
        var upx_Payment_Supplier_Update     = upx_Payment_Supplier.AddUnityChild(UnitySelector.Payment.Supplier.Update);

        var upx_Payment_PaymentList         = upx_Payment.AddUnityChild(UnitySelector.Payment.PaymentList.Default);
        #endregion
    }

    public static PermissionDefinition AddUnityChild(this PermissionDefinition parent, string name)
    {
        return parent.AddChild(name, LocalizableString.Create<GrantManagerResource>(name));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<PaymentsResource>(name);
    }
}
