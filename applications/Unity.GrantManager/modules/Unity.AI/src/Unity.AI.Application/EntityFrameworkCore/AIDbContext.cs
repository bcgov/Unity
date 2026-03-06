using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Unity.AI.Domain;

namespace Unity.AI.EntityFrameworkCore;

[ConnectionStringName(AIDbProperties.ConnectionStringName)]
public class AIDbContext(DbContextOptions<AIDbContext> options) : AbpDbContext<AIDbContext>(options), IAIDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureAI();
    }
}
