namespace Unity.Payments.Codes
{
    public static class CasPaymentRequestStatus
    {
        // Unity Status
        public const string SentToCas = "SentToCas";

        // CAS INVOICE STATUS
        public const string ErrorFromCas = "Error";
        public const string ServiceUnavailable = "ServiceUnavailable"; // Response Error

        public const string NotFound = "NotFound";
        public const string Paid = "Paid";
        public const string Voided = "Voided";

        public const string NeverValidated = "Never Validated"; // Ready for CAS Run
        public const string NotPaid = "Not Paid"; // CAS Ran and was not paid
        public const string FullyPaid = "Fully Paid"; // CAS Ran and was fully paid

        public const string Validated = "Validated"; // Invoice has been validated
        public const string Permanent = "Permanent"; // A fully paid Permanent prepayment.
        public const string Cancelled = "Cancelled"; // Invoice has been cancelled.
    }
}
