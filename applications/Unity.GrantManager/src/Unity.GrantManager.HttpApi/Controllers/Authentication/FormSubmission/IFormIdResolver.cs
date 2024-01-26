using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.Controllers.Authentication.FormSubmission
{
    public interface IFormIdResolver
    {
        Task<Guid?> ResolvedFormIdAsync(AuthorizationFilterContext context);
    }
}
