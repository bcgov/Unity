using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
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

                // Add filtering if provided
                var previewFilterExpression = ValidateFilterExpression(request.Filter, result.ColumnNames);
                if (!string.IsNullOrEmpty(previewFilterExpression))
                {
                    previewQuery += $" AND ({previewFilterExpression})";
                }

                // Add ordering if provided
                var previewOrderByExpression = ValidateOrderByExpression(request.OrderBy, result.ColumnNames);
                if (!string.IsNullOrEmpty(previewOrderByExpression))
                {
                    previewQuery += $" ORDER BY {previewOrderByExpression}";
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

                // Add filtering if provided
                var filterExpression = ValidateFilterExpression(request.Filter, result.ColumnNames);
                if (!string.IsNullOrEmpty(filterExpression))
                {
                    var whereClause = $" WHERE {filterExpression}";
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
                var orderByExpression = ValidateOrderByExpression(request.OrderBy, result.ColumnNames);
                if (!string.IsNullOrEmpty(orderByExpression))
                {
                    baseQuery += $" ORDER BY {orderByExpression}";
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
            var connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync();
            
            try
            {
                // Get all view names in the Reporting schema
                using var getViewsCommand = connection.CreateCommand();
                getViewsCommand.CommandText = @"
                    SELECT viewname 
                    FROM pg_views 
                    WHERE schemaname = 'Reporting'";

                var viewNames = new List<string>();
                using var viewsReader = await getViewsCommand.ExecuteReaderAsync();
                
                while (await viewsReader.ReadAsync())
                {
                    viewNames.Add(viewsReader.GetString(0));
                }
                
                await viewsReader.CloseAsync();

                // Grant SELECT permission on each view to the role
                foreach (var viewName in viewNames)
                {
                    // SECURITY: Validate each identifier read back from pg_views before it is
                    // interpolated into SQL - quoted PostgreSQL identifiers can contain characters
                    // (embedded quotes, semicolons) that would otherwise break out of the quotes below.
                    if (!IsValidPostgreSqlIdentifier(viewName))
                    {
                        throw new ArgumentException($"Invalid view name format: {viewName}", nameof(viewName));
                    }

                    var sql = $"GRANT SELECT ON \"Reporting\".\"{viewName}\" TO \"{role}\"";
                    await dbContext.Database.ExecuteSqlRawAsync(sql);
                }
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

        public async Task<List<string>> GetDatabaseRolesAsync()
        {
            var dbContext = await GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT 
                        rolname || CASE 
                            WHEN rolcanlogin THEN ' (Login Role)'
                            ELSE ' (Group Role)'
                        END as role_display
                    FROM pg_roles 
                    WHERE rolname NOT LIKE 'pg_%' 
                    AND rolname NOT LIKE 'rds%'
                    AND rolname NOT IN ('postgres', 'azure_superuser', 'public', 'azure_pg_admin')
                    ORDER BY rolcanlogin DESC, rolname";

                var roles = new List<string>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    roles.Add(reader.GetString(0));
                }

                return roles;
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        public async Task<List<string>> GetRoleMembershipsAsync()
        {
            var dbContext = await GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT 
                        member_role.rolname || ' → member of → ' || parent_role.rolname as membership_info
                    FROM pg_roles member_role
                    JOIN pg_auth_members m ON member_role.oid = m.member
                    JOIN pg_roles parent_role ON parent_role.oid = m.roleid
                    WHERE member_role.rolname NOT LIKE 'pg_%' 
                    AND member_role.rolname NOT LIKE 'rds%'
                    AND member_role.rolname NOT IN ('postgres', 'azure_superuser', 'public', 'azure_pg_admin')
                    AND parent_role.rolname NOT LIKE 'pg_%' 
                    AND parent_role.rolname NOT LIKE 'rds%'
                    AND parent_role.rolname NOT IN ('postgres', 'azure_superuser', 'public', 'azure_pg_admin')
                    ORDER BY member_role.rolname, parent_role.rolname";

                var memberships = new List<string>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    memberships.Add(reader.GetString(0));
                }

                return memberships;
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        public async Task<List<string>> GetReportingViewsAsync()
        {
            var dbContext = await GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT table_name
                    FROM information_schema.views
                    WHERE table_schema = 'Reporting'
                    ORDER BY table_name";

                var views = new List<string>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    views.Add(reader.GetString(0));
                }

                return views;
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        // Tokens allowed inside a Filter expression: whitespace, string/number literals,
        // quoted or bare identifiers, comparison operators, and parentheses/commas.
        private static readonly Regex FilterTokenRegex = new(
            @"\G(\s+|'(?:[^']|'')*'|\d+(?:\.\d+)?|""[a-zA-Z_][a-zA-Z0-9_]*""|[a-zA-Z_][a-zA-Z0-9_]*|<>|!=|<=|>=|=|<|>|[(),])",
            RegexOptions.Compiled);

        private static readonly HashSet<string> AllowedFilterKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "AND", "OR", "NOT", "IS", "NULL", "TRUE", "FALSE", "LIKE", "ILIKE", "IN", "BETWEEN"
        };

        // Tokens allowed inside an OrderBy expression: whitespace, quoted or bare identifiers, and commas.
        private static readonly Regex OrderByTokenRegex = new(
            @"\G(\s+|""[a-zA-Z_][a-zA-Z0-9_]*""|[a-zA-Z_][a-zA-Z0-9_]*|,)",
            RegexOptions.Compiled);

        private static readonly HashSet<string> AllowedOrderByKeywords = new(StringComparer.OrdinalIgnoreCase) { "ASC", "DESC" };

        /// <summary>
        /// Validates a caller-supplied SQL filter (WHERE) expression against an allow-list of
        /// real view column names. Throws <see cref="ArgumentException"/> if the expression is
        /// not safe to concatenate into a SQL statement.
        /// </summary>
        /// <param name="filter">The raw filter expression, without the "WHERE" keyword.</param>
        /// <param name="validColumns">The set of real column names for the target view.</param>
        /// <returns>The validated filter expression, or an empty string if none was provided.</returns>
        internal static string ValidateFilterExpression(string? filter, IReadOnlyCollection<string> validColumns)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return string.Empty;
            }

            var trimmed = filter.Trim();
            var pos = 0;

            // Tracks whether the most recent non-whitespace token was a column reference, so we
            // can reject "column(" - a bare column name immediately followed by an open paren is
            // function-call syntax in SQL, not a valid column reference, regardless of whether the
            // column happens to share a name with a dangerous PostgreSQL function (pg_sleep, etc).
            var previousTokenWasColumn = false;

            while (pos < trimmed.Length)
            {
                var match = FilterTokenRegex.Match(trimmed, pos);
                if (!match.Success || match.Index != pos || match.Length == 0)
                {
                    throw new ArgumentException($"Filter expression contains an unsupported character at position {pos}.", nameof(filter));
                }

                var token = match.Value;

                if (char.IsWhiteSpace(token[0]))
                {
                    pos += match.Length;
                    continue;
                }

                if (token == "(")
                {
                    if (previousTokenWasColumn)
                    {
                        throw new ArgumentException("Filter expression does not permit function calls.", nameof(filter));
                    }
                }
                else if (token[0] == '"')
                {
                    var identifier = token[1..^1];
                    if (!validColumns.Contains(identifier, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException($"Filter expression references an unknown column '{identifier}'.", nameof(filter));
                    }
                }
                else if (char.IsLetter(token[0]) || token[0] == '_')
                {
                    if (!AllowedFilterKeywords.Contains(token) && !validColumns.Contains(token, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException($"Filter expression references an unknown column or keyword '{token}'.", nameof(filter));
                    }
                }

                previousTokenWasColumn = token[0] == '"' || ((char.IsLetter(token[0]) || token[0] == '_') && !AllowedFilterKeywords.Contains(token));

                pos += match.Length;
            }

            return trimmed;
        }

        /// <summary>
        /// Validates a caller-supplied SQL ORDER BY expression against an allow-list of real
        /// view column names. Throws <see cref="ArgumentException"/> if the expression is not
        /// safe to concatenate into a SQL statement.
        /// </summary>
        /// <param name="orderBy">The raw order-by expression, without the "ORDER BY" keywords.</param>
        /// <param name="validColumns">The set of real column names for the target view.</param>
        /// <returns>The validated order-by expression, or an empty string if none was provided.</returns>
        internal static string ValidateOrderByExpression(string? orderBy, IReadOnlyCollection<string> validColumns)
        {
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return string.Empty;
            }

            var trimmed = orderBy.Trim();
            var pos = 0;
            var expectColumn = true;

            while (pos < trimmed.Length)
            {
                var match = OrderByTokenRegex.Match(trimmed, pos);
                if (!match.Success || match.Index != pos || match.Length == 0)
                {
                    throw new ArgumentException($"Order-by expression contains an unsupported character at position {pos}.", nameof(orderBy));
                }

                var token = match.Value;

                if (!char.IsWhiteSpace(token[0]))
                {
                    if (token == ",")
                    {
                        if (expectColumn)
                        {
                            throw new ArgumentException("Order-by expression is missing a column name.", nameof(orderBy));
                        }
                        expectColumn = true;
                    }
                    else if (token[0] == '"')
                    {
                        var identifier = token[1..^1];
                        if (!validColumns.Contains(identifier, StringComparer.OrdinalIgnoreCase))
                        {
                            throw new ArgumentException($"Order-by expression references an unknown column '{identifier}'.", nameof(orderBy));
                        }
                        expectColumn = false;
                    }
                    else if (!expectColumn && AllowedOrderByKeywords.Contains(token))
                    {
                        // ASC/DESC following a column - no state change needed.
                    }
                    else if (validColumns.Contains(token, StringComparer.OrdinalIgnoreCase))
                    {
                        expectColumn = false;
                    }
                    else
                    {
                        throw new ArgumentException($"Order-by expression references an unknown column '{token}'.", nameof(orderBy));
                    }
                }

                pos += match.Length;
            }

            if (expectColumn)
            {
                throw new ArgumentException("Order-by expression is missing a column name.", nameof(orderBy));
            }

            return trimmed;
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
