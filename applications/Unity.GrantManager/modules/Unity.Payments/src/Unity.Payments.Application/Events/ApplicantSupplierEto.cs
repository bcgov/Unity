using System;

namespace Unity.Payments.Events
{
    [Serializable]
    public class ApplicantSupplierEto
    {
        public Guid? SiteId { get; set; }
        public Guid SupplierId { get; set; }
        public Guid ApplicantId { get; set; }
    }
}
 