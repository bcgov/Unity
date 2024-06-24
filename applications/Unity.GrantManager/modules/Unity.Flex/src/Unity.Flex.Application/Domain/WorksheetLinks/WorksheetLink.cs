using System;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.WorksheetLinks
{
    public class WorksheetLink : FullAuditedAggregateRoot<Guid>, IMultiTenant, ICorrelationEntity
    {
        public Guid? TenantId { get; set; }
        public Guid WorksheetId { get; set; }
        public Guid CorrelationId { get; private set; }
        public string CorrelationProvider { get; private set; } = string.Empty;
        public string UiAnchor { get; private set; } = string.Empty;

        protected WorksheetLink()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public WorksheetLink(Guid id,
            Guid worksheetId,
            Guid correlationId,
            string correlationProvider,
            string uiAnchor)
      : base(id)
        {
            Id = id;
            CorrelationId = correlationId;
            CorrelationProvider = correlationProvider;
            WorksheetId = worksheetId;
            UiAnchor = uiAnchor;
        }
    }
}
