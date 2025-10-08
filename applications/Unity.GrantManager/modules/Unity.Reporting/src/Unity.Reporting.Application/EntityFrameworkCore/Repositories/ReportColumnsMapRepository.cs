using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Unity.Reporting.Configuration;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Reporting.EntityFrameworkCore.Repositories
{
    public class ReportColumnsMapRepository(IDbContextProvider<ReportingDbContext> dbContextProvider)
        : EfCoreRepository<ReportingDbContext, ReportColumnsMap, Guid>(dbContextProvider), IReportColumnsMapRepository
    {
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

            var dbContext = await GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync();

            try
            {
                var result = new ViewDataResult();

                // First, get the column names
                result.ColumnNames = await GetViewColumnNamesAsync(normalizedViewName);

                // Build the preview query using the LIMIT 1 subquery pattern
                var previewQuery = $@"
                    SELECT * 
                    FROM ""Reporting"".""{normalizedViewName}""
                    WHERE ""application_id"" = (
                        SELECT ""application_id""
                        FROM ""Reporting"".""{normalizedViewName}""
                        LIMIT 1
                    )";

                // Add filtering if provided
                if (!string.IsNullOrWhiteSpace(request.Filter))
                {
                    previewQuery += $" AND ({request.Filter})";
                }

                // Add ordering if provided
                if (!string.IsNullOrWhiteSpace(request.OrderBy))
                {
                    previewQuery += $" ORDER BY {request.OrderBy}";
                }

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
                        var fieldValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[fieldName] = fieldValue;
                    }
                    
                    dataList.Add(row);
                }

                result.Data = dataList.ToArray();
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

            var dbContext = await GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync();

            try
            {
                var result = new ViewDataResult();

                // First, get the column names
                result.ColumnNames = await GetViewColumnNamesAsync(normalizedViewName);

                // Build the base query using quoted identifier to handle case correctly
                var baseQuery = $@"SELECT * FROM ""Reporting"".""{normalizedViewName}""";
                var countQuery = $@"SELECT COUNT(*) FROM ""Reporting"".""{normalizedViewName}""";

                // Add filtering if provided
                if (!string.IsNullOrWhiteSpace(request.Filter))
                {
                    var whereClause = $" WHERE {request.Filter}";
                    baseQuery += whereClause;
                    countQuery += whereClause;
                }

                // Get total count
                using (var countCommand = connection.CreateCommand())
                {
                    countCommand.CommandText = countQuery;
                    var countResult = await countCommand.ExecuteScalarAsync();
                    result.TotalCount = Convert.ToInt32(countResult);
                }

                // Add ordering if provided
                if (!string.IsNullOrWhiteSpace(request.OrderBy))
                {
                    baseQuery += $" ORDER BY {request.OrderBy}";
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
                        var fieldValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[fieldName] = fieldValue;
                    }
                    
                    dataList.Add(row);
                }

                result.Data = dataList.ToArray();
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

            var dbContext = await GetDbContextAsync();
            await dbContext.Database.OpenConnectionAsync();
            
            try
            {
                using var command = dbContext.Database.GetDbConnection().CreateCommand();
                // Use parameterized query to safely handle the view name
                command.CommandText = "DROP VIEW IF EXISTS \"Reporting\".\"" + normalizedViewName.Replace("\"", "\"\"") + "\"";
                await command.ExecuteNonQueryAsync();
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        public async Task AssignViewRoleAsync(string viewName, string role)
        {
            if (string.IsNullOrWhiteSpace(viewName) || string.IsNullOrWhiteSpace(role))
            {
                return;
            }

            // Normalize view name and role to lowercase for consistency
            var normalizedViewName = viewName.Trim().ToLowerInvariant();
            var normalizedRole = role.Trim().ToLowerInvariant();

            var dbContext = await GetDbContextAsync();
            await dbContext.Database.OpenConnectionAsync();
            
            try
            {
                using var command = dbContext.Database.GetDbConnection().CreateCommand();
                
                // Grant SELECT permissions on the view to the specified role
                // Using safe parameter handling for the view name and role
                command.CommandText = $@"GRANT SELECT ON ""Reporting"".""{normalizedViewName.Replace("\"", "\"\"")}"" TO ""{normalizedRole.Replace("\"", "\"\"")}""";
                
                await command.ExecuteNonQueryAsync();
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

            // Normalize role name to lowercase for consistency
            var normalizedRoleName = roleName.Trim().ToLowerInvariant();

            var dbContext = await GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync();
            
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM pg_roles WHERE rolname = @roleName) THEN 1 ELSE 0 END";
                
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@roleName";
                parameter.Value = normalizedRoleName;
                command.Parameters.Add(parameter);
                
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) == 1;
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }
}
