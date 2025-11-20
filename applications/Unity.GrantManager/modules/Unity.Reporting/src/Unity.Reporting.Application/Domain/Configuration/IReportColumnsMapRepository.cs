using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Reporting.Configuration;
using Volo.Abp.Domain.Repositories;

namespace Unity.Reporting.Domain.Configuration
{
    public interface IReportColumnsMapRepository : IBasicRepository<ReportColumnsMap, Guid>
    {
        /// <summary>
        /// Finds a ReportColumnsMap by correlation ID and provider.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="correlationProvider"></param>
        /// <returns></returns>
        Task<ReportColumnsMap?> FindByCorrelationAsync(Guid correlationId, string correlationProvider);
        
        /// <summary>
        /// Finds a ReportColumnsMap by view name.
        /// </summary>
        /// <param name="viewName">The name of the view to find the mapping for.</param>
        /// <returns>The ReportColumnsMap if found, null otherwise.</returns>
        Task<ReportColumnsMap?> FindByViewNameAsync(string viewName);
        
        /// <summary>
        /// Checks if a view with the specified name exists in the Reporting schema.
        /// </summary>
        /// <param name="viewName">The name of the view to check for existence.</param>
        /// <returns>True if the view exists, false otherwise.</returns>
        Task<bool> ViewExistsAsync(string viewName);

        /// <summary>
        /// Deletes a view with the specified name from the Reporting schema.
        /// </summary>
        /// <param name="viewName">The name of the view to delete.</param>
        /// <returns>A task representing the asynchronous delete operation.</returns>
        Task DeleteViewAsync(string viewName);

        /// <summary>
        /// Calls the database procedure to generate a view based on the mapping configuration.
        /// </summary>
        /// <param name="correlationId">The correlation ID for the mapping.</param>
        /// <param name="correlationProvider">The correlation provider for the mapping.</param>
        Task GenerateViewAsync(Guid correlationId, string correlationProvider);

        /// <summary>
        /// Retrieves data from a generated view with pagination and filtering.
        /// </summary>
        /// <param name="viewName">The name of the view to query.</param>
        /// <param name="request">The request parameters for data retrieval.</param>
        /// <returns>A ViewDataResult containing the queried data.</returns>
        Task<ViewDataResult> GetViewDataAsync(string viewName, ViewDataRequest request);

        /// <summary>
        /// Retrieves preview data from a generated view showing only the top 1 record based on ApplicationId.
        /// </summary>
        /// <param name="viewName">The name of the view to query.</param>
        /// <param name="request">The request parameters for data retrieval.</param>
        /// <returns>A ViewDataResult containing the preview data (top 1 record).</returns>
        Task<ViewDataResult> GetViewPreviewDataAsync(string viewName, ViewDataRequest request);

        /// <summary>
        /// Retrieves the column names from a generated view.
        /// </summary>
        /// <param name="viewName">The name of the view to query.</param>
        /// <returns>An array of column names.</returns>
        Task<string[]> GetViewColumnNamesAsync(string viewName);

        /// <summary>
        /// Assigns a database role to all views in the reporting schema for access control.
        /// </summary>
        /// <param name="role">The database role name to assign to all views.</param>
        /// <returns></returns>
        Task AssignRoleToAllViewsAsync(string role);

        /// <summary>
        /// Does the specified role exist in the database?
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        Task<bool> RoleExistsAsync(string roleName);

        /// <summary>
        /// Assigns a database role to a specific view in the reporting schema for access control.
        /// </summary>
        /// <param name="role"></param>
        /// <param name="viewName"></param>
        /// <returns></returns>
        Task AssignRoleToViewAsync(string role, string viewName);

        /// <summary>
        /// Retrieves all non-system database roles with their type information.
        /// </summary>
        /// <returns>A list of database role names with type indicators.</returns>
        Task<List<string>> GetDatabaseRolesAsync();

        /// <summary>
        /// Retrieves role membership information showing which roles inherit from other roles.
        /// </summary>
        /// <returns>A list of role membership relationships.</returns>
        Task<List<string>> GetRoleMembershipsAsync();

        /// <summary>
        /// Retrieves all view names in the Reporting schema.
        /// </summary>
        /// <returns>A list of reporting view names.</returns>
        Task<List<string>> GetReportingViewsAsync();
    }
}
