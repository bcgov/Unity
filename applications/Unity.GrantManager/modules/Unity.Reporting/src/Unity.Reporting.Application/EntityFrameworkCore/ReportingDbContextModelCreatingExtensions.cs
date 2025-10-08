using Microsoft.EntityFrameworkCore;
using Unity.Reporting.Domain;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Unity.Reporting.EntityFrameworkCore;

public static class ReportingDbContextModelCreatingExtensions
{
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
