namespace Unity.GrantManager.Settings;
public class BackgroundJobsSettingsDto
{
    public string IntakeResyncExpression { get; set; } = string.Empty;
    public string IntakeNumberOfDays { get; set; } = string.Empty;
    public string CasPaymentsReconciliationProducerExpression { get; set; } = string.Empty;
    public string CasFinancialNotificationSummaryProducerExpression { get; set; } = string.Empty;
}
