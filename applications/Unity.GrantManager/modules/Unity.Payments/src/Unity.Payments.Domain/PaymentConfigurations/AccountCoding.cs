using System;
using System.Linq;
using Unity.Payments.Exceptions;
using Volo.Abp;
namespace Unity.Payments.PaymentConfigurations
{
    public record AccountCoding
    {
        public string MinistryClient { get; private set; } = string.Empty;
        public string Responsibility { get; private set; } = string.Empty;
        public string ServiceLine { get; private set; } = string.Empty;
        public string Stob { get; private set; } = string.Empty;
        public string ProjectNumber { get; private set; } = string.Empty;
        
        private AccountCoding(string ministryClient,
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
            ValidateField(ministryClient, 4, nameof(MinistryClient));  
            ValidateField(responsibility, 3, nameof(Responsibility));
            ValidateField(serviceLine, 6, nameof(serviceLine));
            ValidateField(stob, 8, nameof(stob));
            ValidateField(projectNumber, 10, nameof(projectNumber));

            return new AccountCoding(ministryClient, responsibility, serviceLine, stob, projectNumber);
        }

        private static void ValidateField(string field, uint length, string fieldName)
        {
            bool validNumeric = field.All(Char.IsDigit);

            if (field.Length != length || !validNumeric)
            {
                throw new BusinessException(ErrorConsts.InvalidAccountCodingField)
                    .WithData("field", fieldName)
                    .WithData("length", length);
            }            
        }
    }
}
