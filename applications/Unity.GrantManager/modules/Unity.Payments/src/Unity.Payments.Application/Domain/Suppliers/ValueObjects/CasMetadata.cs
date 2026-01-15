using System;

namespace Unity.Payments.Domain.Suppliers.ValueObjects
{
    public record CasMetadata(DateTime? LastUpdatedInCAS = default)
    {
    }
}