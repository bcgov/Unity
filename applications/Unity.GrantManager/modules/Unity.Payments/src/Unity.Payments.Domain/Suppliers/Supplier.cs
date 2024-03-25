using System;
using System.Collections.ObjectModel;
using Unity.Payments.Correlation;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Payments.Suppliers
{
    public class Supplier : FullAuditedEntity<Guid>, IMultiTenant, ICorrelationEntity
    {
        public Guid? TenantId { get; set; }
        public uint Number { get; set; }
        public virtual Collection<Site> Sites { get; private set; }        

        // External Correlation
        public virtual string CorrelationProvider { get; private set; } = string.Empty;
        public virtual Guid CorrelationId { get; set; }

        protected Supplier()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
            Sites = new Collection<Site>();
        }

        public Supplier(Guid id,
            uint number,
            Guid correlationId,
            string correlationProvider)
           : base(id)
        {
            Number = number;
            CorrelationId = correlationId;
            CorrelationProvider = correlationProvider;
            Sites = new Collection<Site>();
        }
    }
}
