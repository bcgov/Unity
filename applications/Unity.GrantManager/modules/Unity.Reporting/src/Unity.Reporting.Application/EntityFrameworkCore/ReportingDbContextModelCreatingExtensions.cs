using Microsoft.EntityFrameworkCore;
using Unity.Reporting.Domain;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Unity.Reporting.EntityFrameworkCore;

/// <summary>
/// Extension methods for configuring Unity.Reporting entity models in Entity Framework Core.
/// Provides centralized model configuration for all reporting-related entities including
/// table names, schema assignments, and Entity Framework conventions compliance.
/// </summary>
public static class ReportingDbContextModelCreatingExtensions
{
    /// <summary>
    /// Configures Entity Framework Core model mappings for all Unity.Reporting entities.
    /// Defines table names, schema assignments, and applies ABP conventions for consistent
    /// entity configuration across the reporting module database schema.
    /// </summary>
    /// <param name="modelBuilder">The Entity Framework model builder to configure with reporting entity mappings.</param>
    /// <exception cref="ArgumentNullException">Thrown when modelBuilder parameter is null.</exception>
    public static void ConfigureReporting(
        this ModelBuilder modelBuilder)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));

        modelBuilder.Entity<ReportColumnsMap>(b =>
        {
            b.ToTable(ReportingDbProperties.DbTablePrefix + "ReportColumnsMaps",
                ReportingDbProperties.DbSchema);

            b.ConfigureByConvention();
        });
    }
}
