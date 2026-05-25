using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Users;
using Volo.Abp.MultiTenancy;

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
                return $"{given} {surname}".Trim();
            }

            return currentUser.UserName;
        }

        public static async Task<string?> GetCurrentTenantNameAsync(IServiceProvider serviceProvider)
        {
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

            var currentUser = serviceProvider.GetService<ICurrentUser>();
            if (currentUser?.TenantId != null && currentUser.TenantId != Guid.Empty)
            {
                // If a tenant repository is not available in this project, return the tenant id string as fallback.
                return currentUser.TenantId.ToString();
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
