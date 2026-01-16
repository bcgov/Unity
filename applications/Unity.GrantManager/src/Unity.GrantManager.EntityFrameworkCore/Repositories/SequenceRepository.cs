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

            var commandBuilder = new NpgsqlCommandBuilder();
            var safeSchema = commandBuilder.QuoteIdentifier(schema);
            
            // Build SQL command with properly quoted schema identifier to prevent SQL injection
            var sqlCommand = string.Format("SELECT {0}.get_next_sequence_number(@tenantId, @prefix);", safeSchema);
                        
            using var command = connection.CreateCommand();
            // Schema is sanitized via QuoteIdentifier, parameters are properly parameterized
#pragma warning disable S2077 // SQL queries should not be dynamically formatted
            command.CommandText = sqlCommand;
#pragma warning restore S2077
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

            // throw to be handled by the caller's graceful degradation            
            throw new InvalidOperationException(
                $"Failed to generate sequence number for tenant '{tenantId}' with prefix '{prefix}'. ",
                ex);
        }
    }
}