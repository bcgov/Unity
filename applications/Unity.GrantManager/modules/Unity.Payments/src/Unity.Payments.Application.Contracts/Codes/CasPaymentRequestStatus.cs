namespace Unity.Payments.Codes
{
    public static class CasPaymentRequestStatus
    {
        // Unity Status
        public const string SentToCas = "SentToCas";

        // CAS Responses
        public const string ErrorFromCas = "Error";
        public const string NotFound = "NotFound";
        public const string NeverValidated = "Never Validated"; // Ready for CAS Run
        public const string NotPaid = "Not Paid"; // CAS Ran and was not paid
        public const string FullyPaid = "Fully Paid"; // CAS Ran and was fully paid
        public const string Paid = "Paid";
        public const string Voided = "Voided";
    }
}
