using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.GrantManager.Web.Views.Settings.BackgroundJobsSettingGroup;

public class BackgroundJobsViewModel
{
    [ReadOnlyInput]
    [DisabledInput]
    [ReadOnly(true)]
    [Display(Name="Intake Resynch Schedule")]
    public string IntakeResyncExpression { get; set; } = string.Empty;

    [ReadOnlyInput]
    [DisabledInput]
    [ReadOnly(true)]
    [Display(Name="Intake Resynch Number of Days to Check")]
    public string IntakeNumberOfDays { get; set; } = string.Empty;

    [ReadOnlyInput]
    [DisabledInput]
    [ReadOnly(true)]
    [Display(Name="CAS Payments Reconciliation Schedule")]
    public string CasPaymentsReconciliationProducerExpression { get; set; } = string.Empty;
    
    [ReadOnlyInput]
    [DisabledInput]
    [ReadOnly(true)]
    [Display(Name="CAS Payments Financial Notification Schedule")]
    public string CasFinancialNotificationSummaryProducerExpression { get; set; } = string.Empty;
}
