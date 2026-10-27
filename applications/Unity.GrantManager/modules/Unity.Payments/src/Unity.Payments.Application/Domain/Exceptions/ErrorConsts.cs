namespace Unity.Payments.Domain.Exceptions
{
    public static class ErrorConsts
    {
        public const string ZeroPayment = "Payments:Errors:ZeroPayment";
        public const string ConfigurationDoesNotExist = "Payments:Errors:ConfigurationDoesNotExist";
        public const string InvalidAccountCodingField = "Payments:Errors:InvalidAccountCodingFiled";
        public const string L2ApproverRestriction = "Payments:Errors:L2ApproverRestriction";
        public const string MissingSupplierNumber = "Payments:Errors:MissingSupplierNumber";
        public const string MissingSite = "Payments:Errors:MissingSite";
        public const string MissingAccountCoding = "Payments:Errors:MissingAccountCoding";
        public const string PaymentRequestNotFound = "Payments:PaymentRequestNotFound";
        public const string CancellationNotAllowed = "Payments:CancellationNotAllowed";
    }
}
