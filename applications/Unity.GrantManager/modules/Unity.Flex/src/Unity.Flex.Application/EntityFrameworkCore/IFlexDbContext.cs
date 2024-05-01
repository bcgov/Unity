using Unity.Flex.Domain;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore;

[ConnectionStringName(FlexDbProperties.ConnectionStringName)]
public interface IFlexDbContext : IEfCoreDbContext
{

#pragma warning disable S125 // Sections of code should not be commented out
    /* Add DbSet for each Aggregate Root here. Example:
         * DbSet<Question> Questions { get; }
         */
}
#pragma warning restore S125 // Sections of code should not be commented out
