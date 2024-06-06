using System;

namespace Unity.Payments.Suppliers
{
    [Serializable]
    public class CreateSupplierDto : UpsertSupplierDtoBase
    {
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = null!;        
    }
}
