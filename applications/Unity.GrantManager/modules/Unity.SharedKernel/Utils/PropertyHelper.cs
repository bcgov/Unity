using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Unity.Modules.Shared.Utils;

public static class PropertyHelper
{
    /// <summary>
    /// Applies null values from a DTO to a target entity for specified modified fields.
    /// Handles special cases for value types by either creating default instances or setting to null.
    /// </summary>
    /// <typeparam name="TDto">The source DTO type</typeparam>
    /// <typeparam name="TEntity">The target entity type</typeparam>
    /// <param name="sourceDto">The source DTO instance containing potential null values</param>
    /// <param name="targetEntity">The target entity to update</param>
    /// <param name="modifiedFields">Collection of field names that were modified</param>
    public static void ApplyNullValuesFromDto<TDto, TEntity>(
        TDto sourceDto,
        TEntity targetEntity,
        IEnumerable<string> modifiedFields)
        where TDto : class
        where TEntity : class
    {
        var dtoProperties = typeof(TDto).GetProperties();
        var entityProperties = typeof(TEntity).GetProperties().ToDictionary(p => p.Name, p => p);

        foreach (var fieldName in modifiedFields)
        {
            if (dtoProperties.FirstOrDefault(p =>
                string.Equals(p.Name, fieldName, StringComparison.OrdinalIgnoreCase)) is { } dtoProperty)
            {
                var value = dtoProperty.GetValue(sourceDto);
                if (value == null && entityProperties.TryGetValue(dtoProperty.Name, out var entityProperty) && entityProperty.CanWrite)
                {
                    entityProperty.SetValue(targetEntity, entityProperty.PropertyType.IsValueType
                        && Nullable.GetUnderlyingType(entityProperty.PropertyType) == null
                        ? Activator.CreateInstance(entityProperty.PropertyType)
                        : null);
                }
            }
        }
    }
}
