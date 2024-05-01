using System;
using Unity.Payments.Enums;

namespace Unity.Payments.Suppliers
{
    [Serializable]
    public class UpsertSiteDtoBase
    {
        public string Number { get; set; } = null!;
        public PaymentGroup PaymentGroup { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? AddressLine3 { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
    }
}
