using System;
using System.Collections.Generic;

namespace Unity.Payments.Suppliers
{
    public class UpsertSupplierEto
    {
        public Guid? Id { get; set; }
        public string? Number { get; set; }
        public string? Name { get; set; }
        public string? Subcategory { get; set; }
        public string? ProviderId { get; set; }
        public string? BusinessNumber { get; set; }
        public string? Status { get; set; }
        public string? SupplierProtected { get; set; }
        public string? StandardIndustryClassification { get; set; }
        public DateTime? LastUpdatedInCAS { get; set; }
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
        public List<SiteEto> SiteEtos { get; set; } = new List<SiteEto>();
    }
}
