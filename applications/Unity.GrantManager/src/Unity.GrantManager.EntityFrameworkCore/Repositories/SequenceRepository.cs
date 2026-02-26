using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories;

public class SequenceRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider)
    : EfCoreRepository<GrantTenantDbContext, Application, Guid>(dbContextProvider), ISequenceRepository
{
    public async Task<long> GetNextSequenceNumberAsync(string prefix)
    {
        var tenantId = CurrentTenant.Id ?? Guid.Empty;

        var dbContext = await GetDbContextAsync();

        var currentTransaction = dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                $"GetNextSequenceNumberAsync requires an active ambient transaction. " +
                $"TenantId: {tenantId}, Prefix: {prefix}");

        var npgsqlTransaction = currentTransaction.GetDbTransaction() as NpgsqlTransaction
            ?? throw new InvalidOperationException(
                "The current database transaction is not an NpgsqlTransaction.");

        var schema = dbContext.Model.GetDefaultSchema();
        if (string.IsNullOrEmpty(schema))
        {
            // Use 'public' as default for PostgreSQL if no schema is configured
            schema = "public";
        }
        var commandBuilder = new NpgsqlCommandBuilder();
        var safeSchema = commandBuilder.QuoteIdentifier(schema);

        // Schema is sanitized via NpgsqlCommandBuilder.QuoteIdentifier; table name is a known
        // lowercase constant that requires no quoting. All user-supplied values (tenantId, prefix)
        // are passed as parameters, not interpolated into the SQL string.
        const string sql = @"
            INSERT INTO {0}.unity_sequence_counters (tenant_id, prefix, current_value)
            VALUES (@tenantId, @prefix, 1)
            ON CONFLICT (tenant_id, prefix) DO UPDATE
                SET current_value = unity_sequence_counters.current_value + 1
            RETURNING current_value;";

        var sqlWithSchema = string.Format(sql, safeSchema);

        // Use a SAVEPOINT so that a DB error during the upsert rolls back only the counter
        // statement, leaving the outer transaction alive.  This preserves graceful degradation:
        // the caller catches the re-thrown exception and continues with UnityApplicationId = null.
        const string savepointName = "unity_seq_counter";
        await npgsqlTransaction.SaveAsync(savepointName);

        try
        {
            var connection = dbContext.Database.GetDbConnection();
            using var command = connection.CreateCommand();
#pragma warning disable S2077 // Schema identifier is sanitized via NpgsqlCommandBuilder.QuoteIdentifier; all user values are parameterized
            command.CommandText = sqlWithSchema;
#pragma warning restore S2077
            command.Transaction = npgsqlTransaction;
            command.Parameters.Add(new NpgsqlParameter("tenantId", tenantId));
            command.Parameters.Add(new NpgsqlParameter("prefix", prefix));

            var result = await command.ExecuteScalarAsync();
            var sequenceNumber = Convert.ToInt64(result ?? 1L);

            await npgsqlTransaction.ReleaseAsync(savepointName);

            Logger.LogInformation(
                "Successfully generated sequence number {SequenceNumber} for prefix {Prefix} in tenant {TenantId}",
                sequenceNumber, prefix, tenantId);

            return sequenceNumber;
        }
        catch (Exception ex)
        {
            try
            {
                // ROLLBACK TO SAVEPOINT recovers the outer transaction from the aborted state.
                // RELEASE cleans up the savepoint so it can be reused if this method is called
                // again within the same transaction.
                await npgsqlTransaction.RollbackAsync(savepointName);
                await npgsqlTransaction.ReleaseAsync(savepointName);
            }
            catch (Exception rollbackEx)
            {
                Logger.LogError(rollbackEx,
                    "Failed to rollback to savepoint '{SavepointName}'. " +
                    "The outer transaction may be in an invalid state.",
                    savepointName);
            }

            Logger.LogError(ex,
                "Failed to execute sequence counter upsert. " +
                "TenantId: {TenantId}, Prefix: {Prefix}. " +
                "Outer transaction remains alive via savepoint rollback.",
                tenantId, prefix);

            // Re-throw so the caller's graceful degradation (null UnityApplicationId) kicks in.
            throw new InvalidOperationException(
                $"Failed to generate sequence number for tenant '{tenantId}' with prefix '{prefix}'.",
                ex);
        }
    }
}
