using System;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Users;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Web.Middleware
{
    // Accessor used by middleware to resolve current user/tenant information from a service provider.
    internal static class AbpUserTenantAccessor
    {
        public static string? GetCurrentUserName(IServiceProvider serviceProvider)
        {
            var currentUser = serviceProvider.GetService<ICurrentUser>();
            return currentUser?.UserName ?? currentUser?.Name;
        }

        public static string? GetCurrentTenantName(IServiceProvider serviceProvider)
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
            if (currentUser?.TenantId != null)
            {
                return currentUser.TenantId.ToString();
            }

            return null;
        }
    }
}
