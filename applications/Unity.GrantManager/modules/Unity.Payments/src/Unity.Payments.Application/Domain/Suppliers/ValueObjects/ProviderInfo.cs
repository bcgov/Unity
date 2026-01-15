namespace Unity.Payments.Domain.Suppliers.ValueObjects
{
    public record ProviderInfo(string? ProviderId, string? BusinessNumber = default)
    {        
    }
}