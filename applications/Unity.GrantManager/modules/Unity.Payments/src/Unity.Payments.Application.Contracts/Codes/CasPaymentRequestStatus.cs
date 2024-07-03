namespace Unity.Payments.Codes
{
    public class CasPaymentRequestStatus
    {
        public static string SentToCas = "SentToCas";
        public static string ErrorFromCas = "Error";
        public static string NotFound = "NotFound";
        public static string NeverValidated = "Never Validated"; // Ready for CAS Run
    }
}
