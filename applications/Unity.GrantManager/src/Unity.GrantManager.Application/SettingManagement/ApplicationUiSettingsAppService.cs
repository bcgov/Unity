using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.Settings;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;

namespace Unity.GrantManager.SettingManagement;

[Authorize]
public class ApplicationUiSettingsAppService(
    ISettingManager settingManager) : GrantManagerAppService, IApplicationUiSettingsAppService
{
    public async Task<ApplicationUiSettingsDto> GetAsync()
    {
        var settingsDto = new ApplicationUiSettingsDto
        {
            Submission = await SettingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.Submission),
            Assessment = await SettingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.Assessment),
            Project = await SettingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.Project),
            Applicant = await SettingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.Applicant),
            Payments = await SettingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.Payments),
            FundingAgreement = await SettingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.FundingAgreement)
        };

        return settingsDto;
    }

    [Authorize(UnitySettingManagementPermissions.UserInterface)]
    public async Task UpdateAsync(ApplicationUiSettingsDto input)
    {
        if (CurrentTenant.IsAvailable)
        {
            await settingManager.SetForCurrentTenantAsync(SettingsConstants.UI.Tabs.Submission, true.ToString());
            await settingManager.SetForCurrentTenantAsync(SettingsConstants.UI.Tabs.Assessment, input.Assessment.ToString());
            await settingManager.SetForCurrentTenantAsync(SettingsConstants.UI.Tabs.Project, input.Project.ToString());
            await settingManager.SetForCurrentTenantAsync(SettingsConstants.UI.Tabs.Applicant, input.Applicant.ToString());
            await settingManager.SetForCurrentTenantAsync(SettingsConstants.UI.Tabs.Payments, input.Payments.ToString());
            await settingManager.SetForCurrentTenantAsync(SettingsConstants.UI.Tabs.FundingAgreement, input.FundingAgreement.ToString());
        }
    }
}
