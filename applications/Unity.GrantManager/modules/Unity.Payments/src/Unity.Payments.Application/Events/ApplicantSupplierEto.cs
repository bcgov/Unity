using System;
using System.Collections.Generic;
using Unity.Payments.Suppliers;

namespace Unity.Payments.Events
{
    [Serializable]
    public class ApplicantSupplierEto
    {
        public Guid? SiteId { get; set; }
        public Guid SupplierId { get; set; }
        public Guid ApplicantId { get; set; }
        public Dictionary<string, Domain.Suppliers.Site>? ExistingSitesDictionary { get; set; }
        public List<SiteEto> SiteEtos { get; set; } = new List<SiteEto>();
    }
}
 