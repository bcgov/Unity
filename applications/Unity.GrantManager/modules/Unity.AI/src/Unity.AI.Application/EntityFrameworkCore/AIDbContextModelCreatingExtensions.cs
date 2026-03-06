using Microsoft.EntityFrameworkCore;
using Volo.Abp;

namespace Unity.AI.EntityFrameworkCore;

public static class AIDbContextModelCreatingExtensions
{
    public static void ConfigureAI(this ModelBuilder modelBuilder)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));

        // Configure AI entities here as they are introduced.
        // Example:
        // modelBuilder.Entity<SomeEntity>(b =>
        // {
        //     b.ToTable(AIDbProperties.DbTablePrefix + "SomeEntities", AIDbProperties.DbSchema);
        //     b.ConfigureByConvention();
        // });
    }
}
