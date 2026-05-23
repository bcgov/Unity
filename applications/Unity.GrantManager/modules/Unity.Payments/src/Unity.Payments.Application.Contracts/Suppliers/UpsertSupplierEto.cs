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
        public Guid ApplicantId { get; set; }
        public List<SiteEto> SiteEtos { get; set; } = new List<SiteEto>();
        public Guid? ApplicationId { get; set; }
    }
}
