﻿using System;
using System.Text.Json.Serialization;
using Unity.Flex.Domain.Worksheets;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Domain.WorksheetLinks
{
    public class WorksheetLink : AuditedAggregateRoot<Guid>, IMultiTenant, ICorrelationEntity
    {
        public Guid? TenantId { get; set; }
        public Guid WorksheetId { get; set; }
        public Guid CorrelationId { get; private set; }
        public string CorrelationProvider { get; private set; } = string.Empty;
        public string UiAnchor { get; private set; } = string.Empty;
        public uint? Order { get; private set; } = 0;

        [JsonIgnore]
        public virtual Worksheet Worksheet
        {
            set => _worksheet = value;
            get => _worksheet
                   ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Worksheet));
        }
        private Worksheet? _worksheet;

        protected WorksheetLink()
        {
            /* This constructor is for ORMs to be used while getting the entity from the database. */
        }

        public WorksheetLink(Guid id,
            Guid worksheetId,
            Guid correlationId,
            string correlationProvider,
            string uiAnchor,
            uint order = 0)
      : base(id)
        {
            Id = id;
            CorrelationId = correlationId;
            CorrelationProvider = correlationProvider;
            WorksheetId = worksheetId;
            UiAnchor = uiAnchor;
            Order = order;
        }

        public WorksheetLink SetAnchor(string uiAnchor)
        {
            UiAnchor = uiAnchor;
            return this;
        }

        public WorksheetLink SetOrder(uint order)
        {
            Order = order;
            return this;
        }
    }
}
