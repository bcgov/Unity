namespace Unity.Payments.Domain.Suppliers.ValueObjects
{
    public record SupplierBasicInfo(string? Name, string? Number, string? Subcategory = default)
    {        
    }
}