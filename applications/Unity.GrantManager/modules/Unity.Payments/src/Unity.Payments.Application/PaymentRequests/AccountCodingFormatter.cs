using Unity.Payments.Domain.AccountCodings;

namespace Unity.Payments.PaymentRequests
{
    public static class AccountCodingFormatter
    {
        private const string AccountDistributionPostfix = "000000.0000";

        public static string Format(AccountCodingDto? accountCoding)
        {
            if (accountCoding == null)
            {
                return string.Empty;
            }

            if (accountCoding.Responsibility != null
                && accountCoding.ServiceLine != null
                && accountCoding.Stob != null
                && accountCoding.MinistryClient != null
                && accountCoding.ProjectNumber != null)
            {
                return $"{accountCoding.MinistryClient}.{accountCoding.Responsibility}.{accountCoding.ServiceLine}.{accountCoding.Stob}.{accountCoding.ProjectNumber}.{AccountDistributionPostfix}";
            }

            return string.Empty;
        }

        public static string Format(AccountCoding? accountCoding)
        {
            if (accountCoding == null)
            {
                return string.Empty;
            }

            if (accountCoding.Responsibility != null
                && accountCoding.ServiceLine != null
                && accountCoding.Stob != null
                && accountCoding.MinistryClient != null
                && accountCoding.ProjectNumber != null)
            {
                return $"{accountCoding.MinistryClient}.{accountCoding.Responsibility}.{accountCoding.ServiceLine}.{accountCoding.Stob}.{accountCoding.ProjectNumber}.{AccountDistributionPostfix}";
            }

            return string.Empty;
        }
    }
}
