using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Unity.AI.Domain;

namespace Unity.AI.EntityFrameworkCore;

[ConnectionStringName(AIDbProperties.ConnectionStringName)]
public interface IAIDbContext : IEfCoreDbContext
{
    // Add DbSet properties here as entities are introduced
}
