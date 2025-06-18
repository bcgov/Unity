using Unity.Modules.Shared;

namespace Unity.GrantManager.Zones;
public static class DefaultZoneDefinition
{
    public static readonly ZoneGroupDefinition Template = new()
    {
        Name = "ApplicationDetailsMainZone",
        Tabs = [
             new ZoneTabDefinition {
                 Name = UnitySelector.Review.Default,
                 IsEnabled = true,
                 SortOrder = 1,
                 Zones = [
                     new ZoneDefinition {
                         Name = UnitySelector.Review.Approval.Default,
                         ViewComponentType = "AssessmentApprovalViewComponent",
                         IsEnabled = true,
                         SortOrder = 1
                     },
                     new ZoneDefinition {
                         Name = UnitySelector.Review.AssessmentResults.Default,
                         ViewComponentType = "AssessmentResults",
                         IsEnabled = true,
                         SortOrder = 2
                     },
                     new ZoneDefinition {
                         Name = UnitySelector.Review.AssessmentReviewList.Default,
                         ViewComponentType = "ReviewList",
                         IsEnabled = true,
                         SortOrder = 3
                     }
                 ]
             },
             new ZoneTabDefinition {
                 Name = UnitySelector.Project.Default,
                 IsEnabled = true,
                 SortOrder = 2,
                 ElementId = "nav-project-info",
                 Zones = [
                     new ZoneDefinition {
                         Name = UnitySelector.Project.Summary.Default,
                         ViewComponentType = "ProjectInfoViewComponent",
                         IsEnabled = true,
                         SortOrder = 1
                     },
                     new ZoneDefinition {
                         Name = UnitySelector.Project.Location.Default,
                         ViewComponentType = "ProjectLocationViewComponent",
                         IsEnabled = true,
                         SortOrder = 2
                     },
                 ]
             },
             new ZoneTabDefinition {
                 Name = UnitySelector.Applicant.Default,
                 IsEnabled = true,
                 SortOrder = 3,
                 ElementId = "nav-organization-info",
                 Zones = [
                     new ZoneDefinition {
                         Name = UnitySelector.Applicant.Summary.Default,
                         ViewComponentType = "ApplicantInfoViewComponent",
                         IsEnabled = true,
                         SortOrder = 1
                     },
                     new ZoneDefinition {
                         Name = UnitySelector.Applicant.Contact.Default,
                         ViewComponentType = "ApplicantContactInfoViewComponent",
                         IsEnabled = true,
                         SortOrder = 3
                     },
                     new ZoneDefinition {
                         Name = UnitySelector.Applicant.Authority.Default,
                         ViewComponentType = "ApplicantSigningAuthorityViewComponent",
                         IsEnabled = true,
                         SortOrder = 4
                     },
                     new ZoneDefinition {
                         Name = UnitySelector.Applicant.Location.Default,
                         ViewComponentType = "ApplicantPhysicalAddressViewComponent",
                         IsEnabled = true,
                         SortOrder = 5
                     },
                 ]
             },
             new ZoneTabDefinition {
                 Name = UnitySelector.Funding.Default,
                 IsEnabled = true,
                 SortOrder = 4,
                 ElementId = "nav-funding-agreement-info",
                 Zones = [
                     new ZoneDefinition {
                         Name = UnitySelector.Funding.Agreement.Default,
                         ViewComponentType = "FundingAgreementInfoViewComponent",
                         IsEnabled = true,
                         SortOrder = 1
                     }
                 ]
             },
             new ZoneTabDefinition {
                 Name = UnitySelector.Payment.Default,
                 IsEnabled = true,
                 SortOrder = 5,
                 ElementId = "nav-payment-info",
                 Zones = [
                     new ZoneDefinition {
                         Name = UnitySelector.Payment.Summary.Default,
                         ViewComponentType = "PaymentInfoViewComponent",
                         IsEnabled = true,
                         SortOrder = 1
                     },
                     new ZoneDefinition {
                         Name = UnitySelector.Payment.Supplier.Default,
                         ViewComponentType = "SupplierInfoViewComponent",
                         IsEnabled = true,
                         SortOrder = 2
                     },
                     new ZoneDefinition {
                         Name = UnitySelector.Payment.PaymentList.Default,
                         ViewComponentType = "PaymentListViewComponent",
                         IsEnabled = true,
                         SortOrder = 3
                     }
                 ]
             }
        ]
    };
}
