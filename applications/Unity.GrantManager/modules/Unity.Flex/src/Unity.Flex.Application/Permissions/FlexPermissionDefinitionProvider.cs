using Unity.Flex.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Unity.Flex.Permissions;

public class FlexPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(FlexPermissions.GroupName, L("Permission:Flex"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<FlexResource>(name);
    }
}
