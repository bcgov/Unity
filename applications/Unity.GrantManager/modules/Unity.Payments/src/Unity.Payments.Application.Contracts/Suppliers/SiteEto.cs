using System;
namespace Unity.Payments.Suppliers;

[Serializable]
public class SiteEto
{
    public Guid Id { get; set; } = Guid.Empty;
    public string SupplierSiteCode { get; set; } = null!;
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? EmailAddress { get; set; }
    public string? EFTAdvicePref { get; set; }
    public string? BankAccount { get; set; }
    public string? ProviderId { get; set; }
    public string? Status { get; set; }
    public string? SiteProtected { get; set; }
    public DateTime? LastUpdated { get; set; }
}
