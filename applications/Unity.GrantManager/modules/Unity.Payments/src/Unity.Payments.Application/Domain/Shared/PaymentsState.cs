using System.Text.Json.Serialization;

namespace Unity.Payments.Domain.Shared;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentsState
{
    CREATED = 1,
    SUBMITTED = 2,
    APPROVED = 3,
    DECLINED = 4,
    AWAITING_APPROVAL = 5,
}

public enum PaymentApprovalSubState
{
    NONE, // Default state when not in AwaitingApproval
    L1_PENDING,
    L1_APPROVED,
    L1_DECLINED,
    L2_PENDING,
    L2_APPROVED,
    L2_DECLINED,
    L3_PENDING,
    L3_APPROVED,
    L3_DECLINED
}


public static class PaymentsStateGroups
{
    public static readonly PaymentsState[] FinalDecisionStates = [
        PaymentsState.APPROVED,
        PaymentsState.DECLINED,
    ];
}
