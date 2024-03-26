using Unity.Payments.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Unity.Payments.Permissions;

public class PaymentsPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        _ = context.AddGroup(PaymentsPermissions.GroupName, L("Permission:Payments"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<PaymentsResource>(name);
    }
}
