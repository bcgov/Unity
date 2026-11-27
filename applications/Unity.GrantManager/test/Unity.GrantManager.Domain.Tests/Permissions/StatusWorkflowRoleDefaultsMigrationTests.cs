using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Migrations.HostMigrations;
using Unity.GrantManager.Permissions;
using Volo.Abp.Authorization.Permissions;
using Xunit;

namespace Unity.GrantManager.Domain.Tests.Permissions;

public class StatusWorkflowRoleDefaultsMigrationTests
{
    private static readonly string[] ReviewPermissions =
    [
        GrantApplicationPermissions.Reviews.StartInitial,
        GrantApplicationPermissions.Reviews.CompleteInitial
    ];

    [Fact]
    public void Should_Target_The_Host_Context()
    {
        var context = typeof(CorrectStatusWorkflowRoleDefaults).GetCustomAttribute<DbContextAttribute>();
        context.ShouldNotBeNull();
        context.ContextType.ShouldBe(typeof(GrantManagerDbContext));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Should_Delete_Only_The_Retired_Assessor_Role_Grants_In_All_Tenant_Scopes(int existingReviewCount)
    {
        await using var connection = await CreateDatabaseAsync();
        var preservedIds = new List<string>();
        var otherAssessorPermissions = new[]
        {
            GrantApplicationPermissions.StatusWorkflow.Default,
            GrantApplicationPermissions.Assignments.AssignInitial,
            GrantApplicationPermissions.Assessments.Start,
            GrantApplicationPermissions.Assessments.Complete,
            GrantApplicationPermissions.Approvals.Complete,
            GrantApplicationPermissions.Approvals.DeferAfterApproval,
            GrantApplicationPermissions.Approvals.BulkApplicationApproval,
            GrantApplicationPermissions.Comments.Add
        };
        foreach (var tenantId in new string?[] { null, Guid.NewGuid().ToString(), Guid.NewGuid().ToString() })
        {
            foreach (var permission in ReviewPermissions.Take(existingReviewCount))
            {
                await InsertGrantAsync(connection, permission, RolePermissionValueProvider.ProviderName,
                    UnityRoles.Assessor, tenantId);
            }
            foreach (var permission in ReviewPermissions)
            {
                preservedIds.Add(await InsertGrantAsync(connection, permission, RolePermissionValueProvider.ProviderName,
                    UnityRoles.Reviewer, tenantId));
                preservedIds.Add(await InsertGrantAsync(connection, permission, RolePermissionValueProvider.ProviderName,
                    "custom_assessor", tenantId));
                preservedIds.Add(await InsertGrantAsync(connection, permission, UserPermissionValueProvider.ProviderName,
                    Guid.NewGuid().ToString(), tenantId));
            }
            foreach (var permission in otherAssessorPermissions)
            {
                preservedIds.Add(await InsertGrantAsync(connection, permission, RolePermissionValueProvider.ProviderName,
                    UnityRoles.Assessor, tenantId));
            }
        }

        await ExecuteAsync(connection, new CorrectStatusWorkflowRoleDefaults().UpOperations);

        (await GetGrantIdsAsync(connection)).Order(StringComparer.Ordinal).ShouldBe(preservedIds.Order(StringComparer.Ordinal));

        // A local database may already have either or both grants removed; repeating the SQL is a no-op.
        await ExecuteAsync(connection, new CorrectStatusWorkflowRoleDefaults().UpOperations);

        (await GetGrantIdsAsync(connection)).Order(StringComparer.Ordinal).ShouldBe(preservedIds.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Should_Not_Recreate_Permission_Assignments_On_Rollback()
    {
        await using var connection = await CreateDatabaseAsync();
        await InsertGrantAsync(connection, GrantApplicationPermissions.Reviews.StartInitial,
            RolePermissionValueProvider.ProviderName, UnityRoles.Assessor, null);
        var retainedId = await InsertGrantAsync(connection, GrantApplicationPermissions.Approvals.Complete,
            RolePermissionValueProvider.ProviderName, UnityRoles.Assessor, null);
        var migration = new CorrectStatusWorkflowRoleDefaults();

        await ExecuteAsync(connection, migration.UpOperations);
        await ExecuteAsync(connection, migration.DownOperations);

        (await GetGrantIdsAsync(connection)).ShouldBe([retainedId]);
    }

    private static async Task<SqliteConnection> CreateDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        // The correction uses portable DELETE SQL; exercise the actual migration against an isolated table.
        command.CommandText = """
            CREATE TABLE "PermissionGrants" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "ProviderName" TEXT NOT NULL,
                "ProviderKey" TEXT NOT NULL,
                "TenantId" TEXT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();
        return connection;
    }

    private static async Task<string> InsertGrantAsync(SqliteConnection connection, string permission,
        string provider, string providerKey, string? tenantId)
    {
        var id = Guid.NewGuid().ToString();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "PermissionGrants" ("Id", "Name", "ProviderName", "ProviderKey", "TenantId")
            VALUES ($id, $name, $provider, $providerKey, $tenantId);
            """;
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$name", permission);
        command.Parameters.AddWithValue("$provider", provider);
        command.Parameters.AddWithValue("$providerKey", providerKey);
        command.Parameters.AddWithValue("$tenantId", (object?)tenantId ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
        return id;
    }

    private static async Task ExecuteAsync(SqliteConnection connection, IReadOnlyList<MigrationOperation> operations)
    {
        foreach (var operation in operations)
        {
            var sql = operation.ShouldBeOfType<SqlOperation>();
            await using var command = connection.CreateCommand();
            command.CommandText = sql.Sql;
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task<List<string>> GetGrantIdsAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"Id\" FROM \"PermissionGrants\";";
        await using var reader = await command.ExecuteReaderAsync();
        var ids = new List<string>();
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetString(0));
        }
        return ids;
    }
}
