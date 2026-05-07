using System;
using System.Collections.Generic;

namespace Unity.Payments.Suppliers
{
    [Serializable]
    public class UpdateSupplierDto : UpsertSupplierDtoBase
    {
        public Guid Id { get; set; }
        public List<SiteDto> Sites { get; set; } = new List<SiteDto>();
    }
}
