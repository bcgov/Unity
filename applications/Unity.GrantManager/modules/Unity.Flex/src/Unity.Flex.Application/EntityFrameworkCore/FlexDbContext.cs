using Microsoft.EntityFrameworkCore;
using Unity.Flex.Domain;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore;

[ConnectionStringName(FlexDbProperties.ConnectionStringName)]
public class FlexDbContext : AbpDbContext<FlexDbContext>, IFlexDbContext
{

#pragma warning disable S125 // Sections of code should not be commented out
    /* Add DbSet for each Aggregate Root here. Example:
         * public DbSet<Question> Questions { get; set; }
         */

    public FlexDbContext(DbContextOptions<FlexDbContext> options)
#pragma warning restore S125 // Sections of code should not be commented out
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureFlex();
    }
}
