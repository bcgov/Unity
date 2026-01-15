namespace Unity.Payments.Domain.Suppliers.ValueObjects
{
    public record SupplierStatus(string? Status,
            string? SupplierProtected = default,
            string? StandardIndustryClassification = default);
}