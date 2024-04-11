using System;

namespace Unity.Payments.Suppliers
{
    [Serializable]
    public class UpdateSupplierDto : UpsertSupplierDtoBase
    {
        public Guid Id { get; set; }
    }
}
