using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.WorksheetInstances
{
    public class CustomFieldValue : FullAuditedEntity<Guid>, IMultiTenant
    {
        [Column(TypeName = "jsonb")]
        public virtual string CurrentValue { get; private set; } = "{}";

        public Guid? TenantId { get; set; }

        public Guid WorksheetInstanceId { get; private set; }
        public Guid CustomFieldId { get; private set; }

        protected CustomFieldValue()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public CustomFieldValue(Guid id,
            Guid worksheetInstanceId,
            Guid customFieldId,            
            string currentValue)
        {
            Id = id;
            WorksheetInstanceId = worksheetInstanceId;
            CustomFieldId = customFieldId;
            CurrentValue = currentValue;
        }

        public CustomFieldValue SetValue(string currentValue)
        {
            CurrentValue = currentValue;
            return this;
        }
    }
}
