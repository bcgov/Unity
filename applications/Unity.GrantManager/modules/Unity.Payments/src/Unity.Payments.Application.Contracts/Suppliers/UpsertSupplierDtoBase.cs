using System;
namespace Unity.Payments.Suppliers
{
    [Serializable]
    public class UpsertSupplierDtoBase
    {
        public string? Name { get; set; }
        public string? Number { get; set; }
        public string? MailingAddress { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
    }
}
