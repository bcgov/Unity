using System.Collections.Generic;

namespace Unity.GrantManager.History
{
    public static class HistoryConsts
    {
        public const string ExpenseApprovalObject = "Unity.Payments.Domain.PaymentRequests.ExpenseApproval";
        public const string PaymentRequestObject = "Unity.Payments.Domain.PaymentRequests.PaymentRequest";
        public static List<string> PaymentEntityTypeFullNames => [ExpenseApprovalObject, PaymentRequestObject];
    }
}
