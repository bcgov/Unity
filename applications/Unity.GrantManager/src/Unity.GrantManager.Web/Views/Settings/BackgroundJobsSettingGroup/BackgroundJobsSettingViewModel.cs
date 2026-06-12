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
    [Display(Name="Intake Resynch Schedule Description", Description="English description of when intake resyncing occurs")]
    public string IntakeResyncExpressionDescription { get; set; } = string.Empty;

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
    [Display(Name="CAS Payments Reconciliation Schedule Description", Description="English description of when payment reconciliation occurs")]
    public string CasPaymentsReconciliationProducerExpressionDescription { get; set; } = string.Empty;
    
    [ReadOnlyInput]
    [DisabledInput]
    [ReadOnly(true)]
    [Display(Name="CAS Payments Financial Notification Schedule")]
    public string CasFinancialNotificationSummaryProducerExpression { get; set; } = string.Empty;

    [ReadOnlyInput]
    [DisabledInput]
    [ReadOnly(true)]
    [Display(Name="CAS Payments Financial Notification Schedule Description", Description="English description of when financial notifications are sent")]
    public string CasFinancialNotificationSummaryProducerExpressionDescription { get; set; } = string.Empty;

    [ReadOnlyInput]
    [DisabledInput]
    [ReadOnly(true)]
    [Display(Name="Date-Based Notification Schedule")]
    public string DateBasedNotificationScheduleExpression { get; set; } = "0 0 2 * * ?";

    [ReadOnlyInput]
    [DisabledInput]
    [ReadOnly(true)]
    [Display(Name="Date-Based Notification Schedule Description", Description="English description of when date-based scheduled notifications are processed")]
    public string DateBasedNotificationScheduleExpressionDescription { get; set; } = "Every day at 2:00 AM";
}
