using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Volo.Abp.TenantManagement;

namespace Unity.Modules.Shared.PostTenantCreation;

/// <summary>
/// Reads and writes the post-tenant-creation step statuses (e.g. Metabase sync) stored in
/// <c>Tenant.ExtraProperties</c>.
///
/// Each field is its own flat string entry (<c>PostCreationStep_{key}_Name</c>,
/// <c>..._Status</c>, etc.), matching how every other <c>Tenant</c> ExtraProperties entry is a
/// flat string - NOT a single JSON-serialized blob. A tenant's ExtraProperties round-trips
/// through EF Core via ABP's own (Newtonsoft.Json-based) change-tracking/value-comparer for the
/// ExtraProperties column; a string value that itself happens to parse as a JSON array trips it
/// up ("Unable to cast object of type 'JArray' to type 'JObject'") the next time that tenant is
/// saved. Keeping every value a plain scalar avoids ever putting JSON-shaped text into a single
/// ExtraProperties value, sidestepping that entirely.
/// </summary>
public static class TenantPostCreationSectionsExtensions
{
    private const string StepKeysPropertyKey = "PostCreationStepKeys";

    public static List<PostTenantCreationStepStatusEntry> GetPostTenantCreationSections(this Tenant tenant) =>
        GetStepKeys(tenant).Select(key => ReadEntry(tenant, key)).ToList();

    /// <summary>
    /// Seeds a "Waiting" entry for every registered step, in <see cref="IPostTenantCreationStep.Order"/>
    /// order. Called at tenant-creation time, before the post-creation job sequence has had a
    /// chance to run, so the admin UI shows "Waiting" rather than nothing.
    /// </summary>
    public static void SeedPostTenantCreationSections(this Tenant tenant, IEnumerable<IPostTenantCreationStep> steps)
    {
        var orderedSteps = steps.OrderBy(s => s.Order).ToList();

        tenant.ExtraProperties[StepKeysPropertyKey] = string.Join(',', orderedSteps.Select(s => s.Key));

        foreach (var step in orderedSteps)
        {
            WriteEntry(tenant, step.Key, step.StepName, PostTenantCreationStepStatus.Waiting, null, null);
        }
    }

    /// <summary>
    /// Updates (or adds, for a step not present yet - e.g. one shipped after the tenant was
    /// created) the status entry for <paramref name="stepKey"/>.
    /// </summary>
    public static void SetPostTenantCreationStepStatus(
        this Tenant tenant,
        string stepKey,
        string stepName,
        PostTenantCreationStepStatus status,
        string? message,
        DateTime updatedAt)
    {
        var keys = GetStepKeys(tenant);
        if (!keys.Contains(stepKey))
        {
            keys.Add(stepKey);
            tenant.ExtraProperties[StepKeysPropertyKey] = string.Join(',', keys);
        }

        WriteEntry(tenant, stepKey, stepName, status, message, updatedAt);
    }

    private static List<string> GetStepKeys(Tenant tenant) =>
        tenant.ExtraProperties.TryGetValue(StepKeysPropertyKey, out var raw) && raw is string keysCsv
            ? keysCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : [];

    private static PostTenantCreationStepStatusEntry ReadEntry(Tenant tenant, string key)
    {
        var message = GetString(tenant, key, "Message");
        var updatedAtRaw = GetString(tenant, key, "UpdatedAt");

        return new PostTenantCreationStepStatusEntry
        {
            Key = key,
            Name = GetString(tenant, key, "Name") ?? key,
            Status = Enum.TryParse<PostTenantCreationStepStatus>(GetString(tenant, key, "Status"), out var status)
                ? status
                : PostTenantCreationStepStatus.Waiting,
            Message = string.IsNullOrEmpty(message) ? null : message,
            UpdatedAt = !string.IsNullOrEmpty(updatedAtRaw) &&
                DateTime.TryParse(updatedAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var updatedAt)
                    ? updatedAt
                    : null
        };
    }

    private static void WriteEntry(
        Tenant tenant, string key, string name, PostTenantCreationStepStatus status, string? message, DateTime? updatedAt)
    {
        tenant.ExtraProperties[PropertyKey(key, "Name")] = name;
        tenant.ExtraProperties[PropertyKey(key, "Status")] = status.ToString();
        tenant.ExtraProperties[PropertyKey(key, "Message")] = message ?? string.Empty;
        tenant.ExtraProperties[PropertyKey(key, "UpdatedAt")] =
            updatedAt?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;

        // Best-effort cleanup of the earlier (buggy) single-JSON-blob format, if still present
        // on this tenant from before this flat-property format shipped - see the class doc
        // comment for why leaving it in place risks breaking future saves of this tenant.
        tenant.ExtraProperties.Remove("Sections");
    }

    private static string? GetString(Tenant tenant, string key, string field) =>
        tenant.ExtraProperties.TryGetValue(PropertyKey(key, field), out var value) ? value?.ToString() : null;

    private static string PropertyKey(string stepKey, string field) => $"PostCreationStep_{stepKey}_{field}";
}
