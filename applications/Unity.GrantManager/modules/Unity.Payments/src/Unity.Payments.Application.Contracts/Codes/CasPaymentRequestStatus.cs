namespace Unity.Payments.Codes
{
    public class CasPaymentRequestStatus
    {
        // Unity Status
        public static string SentToCas = "SentToCas";

        // CAS Responses
        public static string ErrorFromCas = "Error";
        public static string NotFound = "NotFound";
        public static string NeverValidated = "Never Validated"; // Ready for CAS Run
        public static string NotPaid = "Not Paid"; // CAS Ran and was not paid
        public static string FullyPaid = "Fully Paid"; // CAS Ran and was fully paid
    }
}
