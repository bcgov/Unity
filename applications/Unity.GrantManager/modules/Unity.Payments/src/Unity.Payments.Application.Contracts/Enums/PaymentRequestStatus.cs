using System.Text.Json.Serialization;

namespace Unity.Payments.Enums
{

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentRequestStatus
    {
        L1Pending = 1,
        L1Declined = 2,
        L2Pending = 3,
        L2Declined  = 4,
        L3Pending  = 5,
        L3Declined = 6,
        Submitted = 7,
        Validated = 8,
        NotValidated = 9,
        Paid = 10,
        Failed = 11,
    }
}
