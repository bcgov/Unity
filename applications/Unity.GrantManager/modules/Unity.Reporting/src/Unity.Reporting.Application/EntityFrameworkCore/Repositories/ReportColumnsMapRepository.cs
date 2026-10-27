using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Reporting.Configuration;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Reporting.EntityFrameworkCore.Repositories
{
    public partial class ReportColumnsMapRepository(IDbContextProvider<ReportingDbContext> dbContextProvider)
        : EfCoreRepository<ReportingDbContext, ReportColumnsMap, Guid>(dbContextProvider), IReportColumnsMapRepository
    {
        // Regular expression to validate PostgreSQL identifiers (letters, numbers, underscores, max 63 chars)
        private static readonly Regex PostgreSqlIdentifierRegex = ValidSqlSyntax();
        private const int MaxIdentifierLength = 63;

        public async Task<ReportColumnsMap?> FindByCorrelationAsync(Guid correlationId, string correlationProvider)
        {
            var dbSet = await GetDbSetAsync();
            var lower = correlationProvider.ToLowerInvariant();

            return await dbSet
                .FirstOrDefaultAsync(m => m.CorrelationId.Equals(correlationId)
                    && m.CorrelationProvider.Equals(lower));
        }

        public async Task<ReportColumnsMap?> FindByViewNameAsync(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                return null;
            }

            // Normalize view name to lowercase for consistent comparison
            var normalizedViewName = viewName.Trim().ToLowerInvariant();

            var dbSet = await GetDbSetAsync();

            return await dbSet
                .FirstOrDefaultAsync(m => m.ViewName != null && m.ViewName.ToLower().Equals(normalizedViewName));
        }

        public async Task<bool> ViewExistsAsync(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                return false;
            }

            // Normalize view name to lowercase for consistency
            var normalizedViewName = viewName.Trim().ToLowerInvariant();

            var dbContext = await GetDbContextAsync();
            
            var connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync();
            
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM pg_views WHERE schemaname = 'Reporting' AND viewname = @viewName) THEN 1 ELSE 0 END";
                
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@viewName";
                parameter.Value = normalizedViewName;
                command.Parameters.Add(parameter);
                
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) == 1;
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        public async Task GenerateViewAsync(Guid correlationId, string correlationProvider)
        {
            var dbContext = await GetDbContextAsync();
            
            await dbContext.Database.OpenConnectionAsync();

            try
            {
                FormattableString sql = correlationProvider switch
                {
                    "formversion" => $@"CALL ""Reporting"".generate_formversion_view({correlationId});",
                    "worksheet" => $@"CALL ""Reporting"".generate_worksheet_view({correlationId});",
                    "scoresheet" => $@"CALL ""Reporting"".generate_scoresheet_view({correlationId});",
                    "worksheet_consolidated" => $@"CALL ""Reporting"".generate_consolidated_worksheet_view({correlationId});",
                    "formversion_consolidated" => $@"CALL ""Reporting"".generate_consolidated_formversion_view({correlationId});",
                    _ => throw new ArgumentException($"Unsupported correlation provider: {correlationProvider}"),
                };
                await dbContext.Database.ExecuteSqlAsync(sql);
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        public async Task<ViewDataResult> GetViewPreviewDataAsync(string viewName, ViewDataRequest request)
        {
            // Normalize view name to lowercase for consistency
            var normalizedViewName = viewName.Trim().ToLowerInvariant();

            // SECURITY: Validate the identifier before it is interpolated into SQL below.
            // ViewExistsAsync alone is not sufficient - it only proves a matching row exists in
            // pg_views, not that the name is free of characters that would break out of the
            // quoted identifier it gets embedded in.
            if (!IsValidPostgreSqlIdentifier(normalizedViewName))
            {
                throw new ArgumentException($"Invalid view name format: {viewName}", nameof(viewName));
            }

            var dbContext = await GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync();

            try
            {
                var result = new ViewDataResult
                {
                    // First, get the column names
                    ColumnNames = await GetViewColumnNamesAsync(normalizedViewName)
                };

                // Build the preview query - select the most recently created application
                var previewQuery = $@"
                    SELECT * 
                    FROM ""Reporting"".""{normalizedViewName}""
                    WHERE ""application_id"" = (
                        SELECT v.""application_id""
                        FROM ""Reporting"".""{normalizedViewName}"" v
                        INNER JOIN ""Applications"" a ON v.""application_id"" = a.""Id""
                        ORDER BY a.""CreationTime"" DESC
                        LIMIT 1
                    )";

                // Execute the preview query
                using var dataCommand = connection.CreateCommand();
                dataCommand.CommandText = previewQuery;
                
                using var reader = await dataCommand.ExecuteReaderAsync();
                var dataList = new List<object>();

                while (await reader.ReadAsync())
                {
                    var row = new ExpandoObject() as IDictionary<string, object?>;
                    
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var fieldName = reader.GetName(i);
                        var fieldValue = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                        row[fieldName] = fieldValue;
                    }
                    
                    dataList.Add(row);
                }

                result.Data = [.. dataList];
                result.TotalCount = dataList.Count;
                return result;
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        public async Task<ViewDataResult> GetViewDataAsync(string viewName, ViewDataRequest request)
        {
            // Normalize view name to lowercase for consistency
            var normalizedViewName = viewName.Trim().ToLowerInvariant();

            // SECURITY: Validate the identifier before it is interpolated into SQL below.
            // ViewExistsAsync alone is not sufficient - it only proves a matching row exists in
            // pg_views, not that the name is free of characters that would break out of the
            // quoted identifier it gets embedded in.
            if (!IsValidPostgreSqlIdentifier(normalizedViewName))
            {
                throw new ArgumentException($"Invalid view name format: {viewName}", nameof(viewName));
            }

            var dbContext = await GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync();

            try
            {
                ViewDataResult result = new()
                {
                    // First, get the column names
                    ColumnNames = await GetViewColumnNamesAsync(normalizedViewName)
                };

                // Build the base query using quoted identifier to handle case correctly
                var baseQuery = $@"SELECT * FROM ""Reporting"".""{normalizedViewName}""";
                var countQuery = $@"SELECT COUNT(*) FROM ""Reporting"".""{normalizedViewName}""";

                // Get total count
                using (var countCommand = connection.CreateCommand())
                {
                    countCommand.CommandText = countQuery;
                    var countResult = await countCommand.ExecuteScalarAsync();
                    result.TotalCount = Convert.ToInt32(countResult);
                }

                // Add pagination
                baseQuery += $" OFFSET {request.Skip} LIMIT {request.Take}";

                // Execute the data query
                using var dataCommand = connection.CreateCommand();
                dataCommand.CommandText = baseQuery;
                
                using var reader = await dataCommand.ExecuteReaderAsync();
                var dataList = new List<object>();

                while (await reader.ReadAsync())
                {
                    var row = new ExpandoObject() as IDictionary<string, object?>;
                    
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var fieldName = reader.GetName(i);
                        var fieldValue = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                        row[fieldName] = fieldValue;
                    }
                    
                    dataList.Add(row);
                }

                result.Data = [.. dataList];
                return result;
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        public async Task<string[]> GetViewColumnNamesAsync(string viewName)
        {
            // Normalize view name to lowercase for consistency
            var normalizedViewName = viewName.Trim().ToLowerInvariant();

            var dbContext = await GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT column_name 
                    FROM information_schema.columns 
                    WHERE table_schema = 'Reporting' 
                    AND table_name = @viewName
                    ORDER BY ordinal_position";

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@viewName";
                parameter.Value = normalizedViewName;
                command.Parameters.Add(parameter);

                var columns = new List<string>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    columns.Add(reader.GetString(0));
                }

                return columns.ToArray();
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        public async Task DeleteViewAsync(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName))
            {
                return;
            }

            // Normalize view name to lowercase for consistency
            var normalizedViewName = viewName.Trim().ToLowerInvariant();

            // SECURITY: Validate the identifier to prevent SQL injection
            // This ensures only valid PostgreSQL identifiers are used
            if (!IsValidPostgreSqlIdentifier(normalizedViewName))
            {
                throw new ArgumentException($"Invalid view name format: {viewName}", nameof(viewName));
            }

            var dbContext = await GetDbContextAsync();
            await dbContext.Database.OpenConnectionAsync();
            
            try
            {
                // SECURITY: Use pre-validated identifier in quoted format
                // The identifier has been validated above, and we use quoted format to prevent injection
                var sql = $"DROP VIEW IF EXISTS \"Reporting\".\"{normalizedViewName}\"";
                await dbContext.Database.ExecuteSqlRawAsync(sql);
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        public async Task AssignRoleToViewAsync(string role, string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName) || string.IsNullOrWhiteSpace(role))
            {
                return;
            }

            // Normalize view name and role to lowercase for consistency
            var normalizedViewName = viewName.Trim().ToLowerInvariant();            

            // Validate both identifiers to prevent SQL injection
            if (!IsValidPostgreSqlIdentifier(normalizedViewName))
            {
                throw new ArgumentException($"Invalid view name format: {viewName}", nameof(viewName));
            }

            if (!IsValidPostgreSqlIdentifier(role))
            {
                throw new ArgumentException($"Invalid role name format: {role}", nameof(role));
            }

            var dbContext = await GetDbContextAsync();
            await dbContext.Database.OpenConnectionAsync();
            
            try
            {
                // Use ExecuteSqlRaw with properly quoted identifiers - safer than string concatenation
                var sql = $"GRANT SELECT ON \"Reporting\".\"{normalizedViewName}\" TO \"{role}\"";
                await dbContext.Database.ExecuteSqlRawAsync(sql);
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        public async Task AssignRoleToAllViewsAsync(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return;
            }                        

            // Validate the role identifier to prevent SQL injection
            if (!IsValidPostgreSqlIdentifier(role))
            {
                throw new ArgumentException($"Invalid role name format: {role}", nameof(role));
            }

            var dbContext = await GetDbContextAsync();
            await dbContext.Database.OpenConnectionAsync();

            try
            {
                // One schema-wide grant instead of a per-view loop: no view name is interpolated
                // into SQL, so legacy auto-generated views (hyphenated, mixed-case names) are covered too.
                var sql = $"GRANT SELECT ON ALL TABLES IN SCHEMA \"Reporting\" TO \"{role}\"";
                await dbContext.Database.ExecuteSqlRawAsync(sql);
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        public async Task<bool> RoleExistsAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return false;
            }
            
            var dbContext = await GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync();
            
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM pg_roles WHERE rolname = @roleName) THEN 1 ELSE 0 END";
                
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@roleName";
                parameter.Value = roleName;
                command.Parameters.Add(parameter);
                
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) == 1;
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        public async Task<List<ReportingViewGrant>> GetReportingViewGrantsAsync(string? role)
        {
            var dbContext = await GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync();

            try
            {
                // has_table_privilege counts grants inherited through role membership and PUBLIC,
                // i.e. whether the role can actually read the view. It is strict, so a null role
                // yields NULL (reported as not granted); a non-existent role raises an error.
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT v.viewname,
                        COALESCE(has_table_privilege(@role, format('%I.%I', v.schemaname, v.viewname), 'SELECT'), false)
                    FROM pg_views v
                    WHERE v.schemaname = 'Reporting'
                    ORDER BY v.viewname";

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@role";
                parameter.DbType = System.Data.DbType.String;
                parameter.Value = (object?)role ?? DBNull.Value;
                command.Parameters.Add(parameter);

                var views = new List<ReportingViewGrant>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    views.Add(new ReportingViewGrant(reader.GetString(0), reader.GetBoolean(1)));
                }

                return views;
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        public async Task<ReportingRoleAccess?> GetRoleAccessAsync(string role)
        {
            var dbContext = await GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync();

            try
            {
                // has_schema_privilege errors on a schema that doesn't exist, so check for it first.
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT r.rolcanlogin,
                        CASE WHEN EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'Reporting')
                            THEN has_schema_privilege(r.rolname, 'Reporting', 'USAGE')
                            ELSE false
                        END
                    FROM pg_roles r
                    WHERE r.rolname = @role";

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@role";
                parameter.Value = role;
                command.Parameters.Add(parameter);

                using var reader = await command.ExecuteReaderAsync();
                return await reader.ReadAsync()
                    ? new ReportingRoleAccess(reader.GetBoolean(0), reader.GetBoolean(1))
                    : null;
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        /// <summary>
        /// Validates that a string is a valid PostgreSQL identifier to prevent SQL injection
        /// </summary>
        /// <param name="identifier">The identifier to validate</param>
        /// <returns>True if the identifier is valid, false otherwise</returns>
        internal static bool IsValidPostgreSqlIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            if (identifier.Length > MaxIdentifierLength)
                return false;

            return PostgreSqlIdentifierRegex.IsMatch(identifier);
        }

        [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled)]
        private static partial Regex ValidSqlSyntax();
    }
}
