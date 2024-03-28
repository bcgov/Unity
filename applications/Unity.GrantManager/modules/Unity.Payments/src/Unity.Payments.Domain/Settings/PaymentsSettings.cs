namespace Unity.Payments.Settings;

public static class PaymentsSettings
{
    public const string GroupName = "Payments";
    public const string PaymentThreshold = GroupName + ".PaymentThreshold";

    public static class Localization
    {
        public const string PaymentThresholdDisplayName = "Setting:Payments.ThresholdAmount.DisplayName";
        public const string PaymentThresholdDescription = "Setting:Payments.ThresholdAmount.Description";
    }
}
