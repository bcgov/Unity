using System;
using System.Linq;
using Unity.Payments.Domain.Exceptions;
using Volo.Abp;


namespace Unity.Payments.Domain.AccountCodings
{
    public record AccountCoding
    {
        public Guid? TenantId { get; set; }

        public string MinistryClient { get; set; } = string.Empty;

        public string Responsibility { get; set; } = string.Empty;

        public string ServiceLine { get; set; } = string.Empty;

        public string Stob { get; set; } = string.Empty;

        public string ProjectNumber { get; set; } = string.Empty;

        // Constructor for ORM
        protected AccountCoding()
        {

        }

        public AccountCoding(
            string ministryClient,
            string responsibility,
            string serviceLine,
            string stob,
            string projectNumber)
        {
            MinistryClient = ministryClient;
            Responsibility = responsibility;
            ServiceLine = serviceLine;
            Stob = stob;
            ProjectNumber = projectNumber;
        }

        public static AccountCoding Create(
        string ministryClient,
        string responsibility,
        string serviceLine,
        string stob,
        string projectNumber)
        {
            ValidateField(ministryClient, 3, nameof(MinistryClient), true);
            ValidateField(responsibility, 5, nameof(Responsibility), true);
            ValidateField(serviceLine, 5, nameof(serviceLine), true);
            ValidateField(stob, 4, nameof(stob), true);
            ValidateField(projectNumber, 7, nameof(projectNumber), true);

            return new AccountCoding(ministryClient, responsibility, serviceLine, stob, projectNumber);
        }

        private static void ValidateField(string field, uint length, string fieldName, bool validateAlphanumeric = true)
        {
            bool validAlphanumeric = true;

            if (validateAlphanumeric)
            {
                validAlphanumeric = field.All(char.IsLetterOrDigit);
            }

            if (field.Length != length || !validAlphanumeric)
            {
                throw new BusinessException(ErrorConsts.InvalidAccountCodingField)
                    .WithData("field", fieldName)
                    .WithData("length", length);
            }
        }
    }
}
