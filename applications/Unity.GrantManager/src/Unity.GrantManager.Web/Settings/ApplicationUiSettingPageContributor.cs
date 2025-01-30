﻿using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Volo.Abp.SettingManagement;
using Volo.Abp.SettingManagement.Web.Pages.SettingManagement;

namespace Unity.GrantManager.Web.Settings;

public class ApplicationUiSettingPageContributor : SettingPageContributorBase
{
    public ApplicationUiSettingPageContributor()
    {
        RequiredFeatures(SettingManagementFeatures.Enable);
        RequiredPermissions(UnitySettingManagementPermissions.UserInterface);
    }

    public override Task ConfigureAsync(SettingPageCreationContext context)
    {
        return Task.CompletedTask;
    }
}
