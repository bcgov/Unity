using System;
using System.Threading.Tasks;
  

namespace Unity.GrantManager.Web.Middleware
{
    // Accessor used by middleware to resolve current user/tenant information from a service provider.
    internal static class AbpUserTenantAccessor
    {
        public static string? GetCurrentUserName(IServiceProvider serviceProvider)
        {
            return SharedKernel.Utilities.AbpUserTenantAccessor.GetCurrentUserName(serviceProvider);
        }

        public static async Task<string?> GetCurrentTenantNameAsync(IServiceProvider serviceProvider)
        {
            return await SharedKernel.Utilities.AbpUserTenantAccessor.GetCurrentTenantNameAsync(serviceProvider);
        }

        public static string? GetCurrentTenantId(IServiceProvider serviceProvider)
        {
            return SharedKernel.Utilities.AbpUserTenantAccessor.GetCurrentTenantId(serviceProvider);
        }
    }
}