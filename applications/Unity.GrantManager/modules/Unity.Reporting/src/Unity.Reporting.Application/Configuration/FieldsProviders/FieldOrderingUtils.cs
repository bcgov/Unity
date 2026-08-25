using System.Collections.Generic;
using Unity.Reporting.Domain.Configuration;

namespace Unity.Reporting.Configuration.FieldsProviders
{
    /// <summary>
    /// Shared utility for stamping fields with their calculated overall Source Order.
    /// </summary>
    internal static class FieldOrderingUtils
    {
        /// <summary>
        /// Stamps each field with its 1-based position in the list as <see cref="FieldPathTypeDto.SourceOrder"/>.
        /// Must be called after a provider has applied its final field ordering (e.g., worksheet name,
        /// section, field layout, checkbox option, or data grid column order), since this simply reflects
        /// list position. Mutates the fields in place.
        /// </summary>
        internal static void AssignSourceOrder(IReadOnlyList<FieldPathTypeDto> fields)
        {
            for (var i = 0; i < fields.Count; i++)
            {
                fields[i].SourceOrder = i + 1;
            }
        }
    }
}
