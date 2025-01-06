using System;

namespace Unity.Payments.Events
{
    [Serializable]
    public class ApplicantSupplierEto
    {
        public Guid SupplierId { get; set; }
        public Guid ApplicantId { get; set; }
    }
}
 