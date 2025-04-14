using System;
using System.Linq;
using Unity.Payments.Domain.Exceptions;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;


namespace Unity.Payments.Domain.AccountCodings
{
    public class AccountCoding : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        // Account Coding Fields
        public virtual string? MinistryClient { get; private set; } = string.Empty;
        public virtual string? Responsibility { get; private set; } = string.Empty;
        public virtual string? ServiceLine { get; private set; } = string.Empty;
        public virtual string? Stob { get; private set; } = string.Empty;
        public virtual string? ProjectNumber { get; private set; } = string.Empty;

        // Constructor for ORM
        protected AccountCoding()
        {

        }

        public AccountCoding(
            string? ministryClient,
            string? responsibility,
            string? serviceLine,
            string? stob,
            string? projectNumber) 
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
            ValidateField(ministryClient, 3, nameof(MinistryClient), false);
            ValidateField(responsibility, 5, nameof(Responsibility), false);
            ValidateField(serviceLine, 5, nameof(serviceLine));
            ValidateField(stob, 4, nameof(stob));
            ValidateField(projectNumber, 7, nameof(projectNumber));

            return new AccountCoding(ministryClient, responsibility, serviceLine, stob, projectNumber);
        }

        private static void ValidateField(string field, uint length, string fieldName, bool validAlphanumeric = true)
        {

            if (validAlphanumeric)
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
