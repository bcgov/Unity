using System;

namespace Unity.Payments.Suppliers
{
    [Serializable]
    public class UpsertSupplierDtoBase
    {
        public string? Name { get; set; }
        public string? Number { get; set; }
        public string? Subcategory { get; set; }
        public string? ProviderId { get; set; }
        public string? BusinessNumber { get; set; }
        public string? Status { get; set; }
        public string? SupplierProtected { get; set; }
        public string? StandardIndustryClassification { get; set; }
        public DateTime? LastUpdatedInCAS { get; set; }
        public string? MailingAddress { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
    }
}
