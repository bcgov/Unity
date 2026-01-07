using Unity.Payments.Domain.Exceptions;
using Volo.Abp;
using System;
using Volo.Abp.Domain.Entities.Auditing;
using System.Linq;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.Domain.AccountCodings
{
    public class AccountCoding : AuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }
        public string MinistryClient { get; private set; }
        public string Responsibility { get; private set; }
        public string ServiceLine { get; private set; }
        public string Stob { get; private set; }
        public string ProjectNumber { get; private set; }

        public string? Description { get; private set; }

        public AccountCoding()
        {
            MinistryClient = string.Empty;
            Responsibility = string.Empty;
            ServiceLine = string.Empty;
            Stob = string.Empty;
            ProjectNumber = string.Empty;
            Description = null;
        }
        private AccountCoding(string ministryClient,
            string responsibility,
            string serviceLine,
            string stob,
            string projectNumber,
            string? description = null)            
        {
            MinistryClient = ministryClient;
            Responsibility = responsibility;
            ServiceLine = serviceLine;
            Stob = stob;
            ProjectNumber = projectNumber;
            Description = description;
        }

        public static AccountCoding Create(
        string ministryClient,
        string responsibility,
        string serviceLine,
        string stob,
        string projectNumber,
        string? description = null)
        {
            ValidateField(ministryClient, 3, nameof(MinistryClient), false);  
            ValidateField(responsibility, 5, nameof(Responsibility), false);
            ValidateField(serviceLine, 5, nameof(serviceLine), true);
            ValidateField(stob, 4, nameof(stob), true);
            ValidateField(projectNumber, 7, nameof(projectNumber), true);

            if (!string.IsNullOrWhiteSpace(description) && description.Length > 35)
            {
                throw new BusinessException(ErrorConsts.InvalidAccountCodingField)
                    .WithData("field", nameof(Description))
                    .WithData("length", 35);
            }

            return new AccountCoding(ministryClient, responsibility, serviceLine, stob, projectNumber, description);
        }

        private static void ValidateField(string field, uint length, string fieldName, bool validateAlphanumeric)
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