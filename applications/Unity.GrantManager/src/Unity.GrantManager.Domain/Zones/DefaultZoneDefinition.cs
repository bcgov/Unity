using Unity.GrantManager.Settings;

namespace Unity.GrantManager.Zones;
public static class DefaultZoneDefinition
{
    public static readonly ZoneGroupDefinition Template = new()
    {
        Name = "MainZone",
        Tabs = [
             new ZoneTabDefinition {
                 Name = SettingsConstants.UI.Tabs.Assessment,
                 IsEnabled = true,
                 SortOrder = 1,
                 Zones = [
                     new ZoneDefinition {
                         Name = SettingsConstants.UI.Tabs.Assessment + ".AssessmentApproval",
                         ViewComponentType = "AssessmentApprovalViewComponent",
                         IsEnabled = true,
                         IsConfigurationDisabled = true,
                         SortOrder = 1
                     },
                     new ZoneDefinition {
                         Name = SettingsConstants.UI.Tabs.Assessment + ".AssessmentResults",
                         ViewComponentType = "AssessmentResults",
                         IsEnabled = true,
                         SortOrder = 2
                     },
                     new ZoneDefinition {
                         Name = SettingsConstants.UI.Tabs.Assessment + ".ReviewList",
                         ViewComponentType = "ReviewList",
                         IsEnabled = true,
                         SortOrder = 3
                     }
                 ]
             },
             new ZoneTabDefinition {
                 Name = SettingsConstants.UI.Tabs.Project,
                 IsEnabled = true,
                 SortOrder = 2,
                 ElementId = "nav-project-info",
                 Zones = [
                     new ZoneDefinition {
                         Name = SettingsConstants.UI.Tabs.Project + ".ProjectInfo",
                         ViewComponentType = "ProjectInfoViewComponent",
                         IsEnabled = true,
                         SortOrder = 1
                     },
                     new ZoneDefinition {
                         Name = SettingsConstants.UI.Tabs.Project + ".ProjectLocation",
                         ViewComponentType = "ProjectLocationViewComponent",
                         IsEnabled = true,
                         IsConfigurationDisabled = true,
                         SortOrder = 2
                     },
                 ]
             },
             new ZoneTabDefinition {
                 Name = SettingsConstants.UI.Tabs.Applicant,
                 IsEnabled = true,
                 SortOrder = 3,
                 ElementId = "nav-organization-info",
                 Zones = [
                     new ZoneDefinition {
                         Name = SettingsConstants.UI.Tabs.Applicant + ".ApplicantInfo",
                         ViewComponentType = "ApplicantInfoViewComponent",
                         IsEnabled = true,
                         SortOrder = 1
                     },
                     new ZoneDefinition {
                         Name = SettingsConstants.UI.Tabs.Applicant + ".ContactInfo",
                         ViewComponentType = "ApplicantContactInfoViewComponent",
                         IsEnabled = true,
                         IsConfigurationDisabled = true,
                         SortOrder = 2
                     },
                     new ZoneDefinition {
                         Name = SettingsConstants.UI.Tabs.Applicant + ".SigningAuthority",
                         ViewComponentType = "ApplicantSigningAuthorityViewComponent",
                         IsEnabled = true,
                         IsConfigurationDisabled = true,
                         SortOrder = 3
                     },
                     new ZoneDefinition {
                         Name = SettingsConstants.UI.Tabs.Applicant + ".PhysicalAddress",
                         ViewComponentType = "ApplicantPhysicalAddressViewComponent",
                         IsEnabled = true,
                         IsConfigurationDisabled = true,
                         SortOrder = 4
                     },
                 ]
             },
             new ZoneTabDefinition {
                 Name = SettingsConstants.UI.Tabs.FundingAgreement,
                 IsEnabled = true,
                 SortOrder = 4,
                 ElementId = "nav-funding-agreement-info",
                 Zones = [
                     new ZoneDefinition {
                         Name = SettingsConstants.UI.Tabs.FundingAgreement + ".FundingAgreementInfo",
                         ViewComponentType = "FundingAgreementInfoViewComponent",
                         IsEnabled = true,
                         SortOrder = 1
                     }
                 ]
             },
             new ZoneTabDefinition {
                 Name = SettingsConstants.UI.Tabs.Payments,
                 IsEnabled = true,
                 SortOrder = 5,
                 ElementId = "nav-payment-info",
                 Zones = [
                     new ZoneDefinition {
                         Name = SettingsConstants.UI.Tabs.Payments + ".PaymentInfo",
                         ViewComponentType = "PaymentInfoViewComponent",
                         IsEnabled = true,
                         SortOrder = 1
                     },
                      new ZoneDefinition {
                         Name = SettingsConstants.UI.Tabs.Payments + ".PaymentList",
                         ViewComponentType = "PaymentListViewComponent",
                         IsEnabled = true,
                         IsConfigurationDisabled = true,
                         SortOrder = 2
                     }
                 ]
             }
        ]
    };
}
