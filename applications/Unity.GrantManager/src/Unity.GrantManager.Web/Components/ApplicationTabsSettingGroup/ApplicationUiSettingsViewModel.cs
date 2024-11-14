using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Web.Components.ApplicationTabsSettingGroup;

public class ApplicationUiSettingsViewModel
{
    [Display(Name = "Setting:GrantManager.UI.Tabs.Submission.DisplayName")]
    public bool Submission { get; set; }

    [Display(Name = $"Setting:GrantManager.UI.Tabs.Assessment.DisplayName")]
    public bool Assessment { get; set; }

    [Display(Name = "Setting:GrantManager.UI.Tabs.Project.DisplayName")]
    public bool Project { get; set; }

    [Display(Name = "Setting:GrantManager.UI.Tabs.Applicant.DisplayName")]
    public bool Applicant { get; set; }

    [Display(Name = "Setting:GrantManager.UI.Tabs.Payments.DisplayName")]
    public bool Payments { get; set; }

    [Display(Name = "Setting:GrantManager.UI.Tabs.FundingAgreement.DisplayName")]
    public bool FundingAgreement { get; set; }
}
