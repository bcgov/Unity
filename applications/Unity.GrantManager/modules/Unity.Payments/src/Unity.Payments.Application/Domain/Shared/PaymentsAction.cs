using System.Text.Json.Serialization;

namespace Unity.Payments.Domain.Shared;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentsAction
{
    // Opening Actions
    Create,
    Submit,
    AwaitingApproval,

    // Closing Actions
    Approve,
    Decline,
}

public enum PaymentApprovalAction
{
    None,
    L1Approve,
    L1Decline,
    L2Approve,
    L2Decline,
    L3Approve,
    L3Decline,
    Submit
}
