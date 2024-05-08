using System;

namespace Unity.Payments.Suppliers
{
    public class UpsertSupplierEto
    {
        public string? SupplierNumber { get; set; } 
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
    }
}
