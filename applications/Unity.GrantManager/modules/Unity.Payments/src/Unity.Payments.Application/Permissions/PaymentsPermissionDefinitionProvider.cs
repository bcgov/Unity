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
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<PaymentsResource>(name);
    }
}
