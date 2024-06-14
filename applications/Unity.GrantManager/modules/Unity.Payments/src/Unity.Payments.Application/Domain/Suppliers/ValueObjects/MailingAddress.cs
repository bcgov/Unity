namespace Unity.Payments.Domain.Suppliers.ValueObjects
{
    public class MailingAddress
    {
        public MailingAddress(string? addressLine,
            string? city,
            string? province,
            string? postalCode)
        {
            AddressLine = addressLine;
            City = city;
            Province = province;
            PostalCode = postalCode;
        }

        public string? AddressLine { get; internal set; }
        public string? City { get; internal set; }
        public string? Province { get; internal set; }
        public string? PostalCode { get; internal set; }
    }
}
