using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Users;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace Unity.SharedKernel.Utilities
{
    public static class AbpUserTenantAccessor
    {
        public static string? GetCurrentUserName(IServiceProvider serviceProvider)
        {
            var currentUser = serviceProvider.GetService<ICurrentUser>();
            if (currentUser == null) return null;

            var given = currentUser.Name;
            var surname = currentUser.SurName;
            if (!string.IsNullOrWhiteSpace(given) || !string.IsNullOrWhiteSpace(surname))
            {
                var fullName = $"{given} {surname}".Trim();
                if (!string.IsNullOrWhiteSpace(fullName)) return fullName;
            }

            var userName = currentUser.UserName;
            return string.IsNullOrWhiteSpace(userName) ? null : userName;
        }

        public static async Task<string?> GetCurrentTenantNameAsync(IServiceProvider serviceProvider)
        {
            // Try resolving ICurrentTenant first
            var currentTenant = serviceProvider.GetService<ICurrentTenant>();

            if (currentTenant != null)
            {
                var nameProp = currentTenant.GetType().GetProperty("Name");
                if (nameProp != null)
                {
                    var value = nameProp.GetValue(currentTenant) as string;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }

            // Fall back to current user tenant id if available
            var currentUser = serviceProvider.GetService<ICurrentUser>();
            if (currentUser?.TenantId != null)
            {
                try
                {
                    // Get the current tenant id (returns Guid.Empty when not set)
                    if (currentUser.TenantId != Guid.Empty)
                    {
                        // Try tenant repository (may not be registered in some host contexts)
                        var tenantRepo = serviceProvider.GetService<ITenantRepository?>();
                        if (tenantRepo != null)
                        {
                            var tenant = await tenantRepo.FindAsync(currentUser.TenantId.Value);
                            if (tenant != null)
                            {
                                return tenant.Name;
                            }
                        }
                    }
                }
                catch
                {
                    // Swallow any errors and fall back to other methods
                }
            }

            return null;
        }

        public static string? GetCurrentTenantId(IServiceProvider serviceProvider)
        {
            var currentUser = serviceProvider.GetService<ICurrentUser>();
            if (currentUser?.TenantId != null)
            {
                return currentUser.TenantId.ToString();
            }

            return null;
        }
    }
}
