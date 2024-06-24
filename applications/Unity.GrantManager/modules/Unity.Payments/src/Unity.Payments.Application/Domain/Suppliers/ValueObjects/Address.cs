namespace Unity.Payments.Domain.Suppliers.ValueObjects
{
    public class Address
    {
        public Address(string? addressLine1,
            string? addressLine2,
            string? addressLine3,
            string? country,
            string? city,
            string? province,
            string? postalCode)
        {
            AddressLine1 = addressLine1;
            AddressLine2 = addressLine2;
            AddressLine3 = addressLine3;
            Country = country;
            City = city;
            Province = province;
            PostalCode = postalCode;
        }

        public string? AddressLine1 { get; internal set; }
        public string? AddressLine2 { get; internal set; }
        public string? AddressLine3 { get; internal set; }
        public string? Country { get; internal set; }
        public string? City { get; internal set; }
        public string? Province { get; internal set; }
        public string? PostalCode { get; internal set; }
    }
}
