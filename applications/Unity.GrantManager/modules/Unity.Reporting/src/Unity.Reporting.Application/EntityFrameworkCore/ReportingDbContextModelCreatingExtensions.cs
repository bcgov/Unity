using Microsoft.EntityFrameworkCore;
using Volo.Abp;

namespace Unity.Reporting.EntityFrameworkCore;

public static class ReportingDbContextModelCreatingExtensions
{
    public static void ConfigureReporting(
        this ModelBuilder modelBuilder)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));
    }
}
