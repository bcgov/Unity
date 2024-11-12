using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Settings;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;

namespace Unity.GrantManager.SettingManagement;

//[Authorize(SettingManagementPermissions.UserInterface)]
public class ApplicationUiSettingsAppService : GrantManagerAppService, IApplicationUiSettingsAppService
{
    private readonly ISettingProvider _settingProvider;
    private readonly ISettingManager _settingManager;

    public ApplicationUiSettingsAppService(
        ISettingProvider settingProvider, 
        ISettingManager settingManager)
    {
        _settingProvider = settingProvider;
        _settingManager = settingManager;
    }

    public async Task<ApplicationUiSettingsDto> GetAsync()
    {
        var settingsDto = new ApplicationUiSettingsDto
        {
            Submission = await _settingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.Submission, true),
            Assessment = await _settingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.Assessment, true),
            Project = await _settingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.Project, true),
            Applicant = await _settingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.Applicant, true),
            Payments = await _settingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.Payments, true),
            FundingAgreement = await _settingProvider.GetAsync<bool>(SettingsConstants.UI.Tabs.FundingAgreement, false),
        };

        return settingsDto;
    }

    public async Task UpdateAsync(ApplicationUiSettingsDto input)
    {
        await _settingManager.SetForCurrentTenantAsync(SettingsConstants.UI.Tabs.Submission, input.Submission.ToString());
        await _settingManager.SetForCurrentTenantAsync(SettingsConstants.UI.Tabs.Assessment, input.Assessment.ToString());
        await _settingManager.SetForCurrentTenantAsync(SettingsConstants.UI.Tabs.Project, input.Project.ToString());
        await _settingManager.SetForCurrentTenantAsync(SettingsConstants.UI.Tabs.Applicant, input.Applicant.ToString());
        await _settingManager.SetForCurrentTenantAsync(SettingsConstants.UI.Tabs.Payments, input.Payments.ToString());
        await _settingManager.SetForCurrentTenantAsync(SettingsConstants.UI.Tabs.FundingAgreement, input.FundingAgreement.ToString());
    }
}
