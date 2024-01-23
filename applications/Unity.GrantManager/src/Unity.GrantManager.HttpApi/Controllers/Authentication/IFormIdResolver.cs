using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.Controllers.Authentication
{
    public interface IFormIdResolver
    {        
        Task<Guid?> ResolvedFormIdAsync(AuthorizationFilterContext context);
    }
}
