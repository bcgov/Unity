using Unity.GrantManager.Settings;

namespace Unity.GrantManager.Zones;
public static class DefaultZoneDefinition
{
    public static readonly ZoneGroupDefinition Template = new()
    {
        Name = "MainZone",
        Zones = [
         new ZoneTabDefinition {
             Name = SettingsConstants.UI.Tabs.Assessment,
             DisplayName = null,
             IsEnabled = true,
             SortOrder = 1,
             Zones = [
                 new ZoneDefinition {
                     Name = SettingsConstants.UI.Tabs.Assessment + ".AssessmentResults",
                     ViewComponentType = "AssessmentResults",
                     IsEnabled = true,
                     SortOrder = 1,
                     DisplayName = null
                 },
                 new ZoneDefinition {
                     Name = SettingsConstants.UI.Tabs.Assessment + ".ReviewList",
                     ViewComponentType = "ReviewList",
                     IsEnabled = true,
                     SortOrder = 2,
                     DisplayName = null
                 }
             ]
         },
         new ZoneTabDefinition {
             Name = SettingsConstants.UI.Tabs.Project,
             DisplayName = null,
             IsEnabled = true,
             SortOrder = 2,
             ElementId = "nav-project-info",
             Zones = [
                 new ZoneDefinition {
                     Name = SettingsConstants.UI.Tabs.Assessment + ".ProjectInfoViewComponent",
                     ViewComponentType = "ProjectInfoViewComponent",
                     IsEnabled = true,
                     SortOrder = 1,
                     DisplayName = null,
                 }
             ]
         },
         new ZoneTabDefinition {
             Name = SettingsConstants.UI.Tabs.Applicant,
             DisplayName = null,
             IsEnabled = true,
             SortOrder = 3,
             ElementId = "nav-organization-info",
             Zones = [
                 new ZoneDefinition {
                     Name = SettingsConstants.UI.Tabs.Assessment + ".ApplicantInfoViewComponent",
                     ViewComponentType = "ApplicantInfoViewComponent",
                     IsEnabled = true,
                     SortOrder = 1,
                     DisplayName = null,
                 }
             ]
         },
         new ZoneTabDefinition {
             Name = SettingsConstants.UI.Tabs.FundingAgreement,
             DisplayName = null,
             IsEnabled = true,
             SortOrder = 4,
             ElementId = "nav-funding-agreement-info",
             Zones = [
                 new ZoneDefinition {
                     Name = SettingsConstants.UI.Tabs.Assessment + ".FundingAgreementInfoViewComponent",
                     ViewComponentType = "FundingAgreementInfoViewComponent",
                     IsEnabled = true,
                     SortOrder = 1,
                     DisplayName = null,
                 }
             ]
         },
         new ZoneTabDefinition {
             Name = SettingsConstants.UI.Tabs.Payments,
             DisplayName = null,
             IsEnabled = true,
             SortOrder = 5,
             ElementId = "nav-payment-info",
             Zones = [
                 new ZoneDefinition {
                     Name = SettingsConstants.UI.Tabs.Payments + ".PaymentInfoViewComponent",
                     ViewComponentType = "PaymentInfoViewComponent",
                     IsEnabled = true,
                     SortOrder = 1,
                     DisplayName = null,
                 }
             ]
         }
     ]
    };
}
