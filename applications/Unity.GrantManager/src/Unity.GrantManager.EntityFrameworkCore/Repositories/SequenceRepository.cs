using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Repositories;

public class SequenceRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider, IUnitOfWorkManager unitOfWorkManager) : EfCoreRepository<GrantTenantDbContext, Application, Guid>(dbContextProvider), ISequenceRepository
{
    
    public async Task<long> GetNextSequenceNumberAsync(string prefix)
    {
        var tenantId = CurrentTenant.Id ?? Guid.Empty;
        
        try
        {
            // Create a new isolated unit of work to prevent transaction pollution
            using var uow = unitOfWorkManager.Begin(
                requiresNew: true, 
                isTransactional: true
            );
            
            var dbContext = await GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            
            var schema = dbContext.Model.GetDefaultSchema();
            
            if (string.IsNullOrEmpty(schema))
            {
                // Use 'public' as default for PostgreSQL if no schema is configured
                schema = "public";
            }          
            
            using var command = connection.CreateCommand();
            // Always use explicit schema qualification
            command.CommandText = $"SELECT {schema}.get_next_sequence_number(@tenantId, @prefix);";
            command.Parameters.Add(new NpgsqlParameter("tenantId", tenantId));
            command.Parameters.Add(new NpgsqlParameter("prefix", prefix));
            
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }
                
            var result = await command.ExecuteScalarAsync();
            var sequenceNumber = (long)(result ?? 1L);
            
            Logger.LogInformation(
                "Successfully generated sequence number {SequenceNumber} for prefix {Prefix} in tenant {TenantId}",
                sequenceNumber, prefix, tenantId);
                
            await uow.CompleteAsync();
            return sequenceNumber;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, 
                "Failed to execute get_next_sequence_number function. " +
                "TenantId: {TenantId}, Prefix: {Prefix}. " +
                "This error is isolated and will not affect the main transaction.",
                tenantId, prefix);
            
            // Re-throw to be handled by the caller's graceful degradation
            throw;
        }
    }
}