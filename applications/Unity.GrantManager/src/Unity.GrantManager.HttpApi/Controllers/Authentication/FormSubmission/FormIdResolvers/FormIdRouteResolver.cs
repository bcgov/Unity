using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;

namespace Unity.GrantManager.Controllers.Authentication.FormSubmission.FormIdResolvers
{
    public class FormIdRouteResolver : IFormIdResolver
    {
        public async Task<Guid?> ResolvedFormIdAsync(AuthorizationFilterContext context)
        {
            var routeFormId = context.HttpContext!.GetRouteValue(AuthConstants.FormIdRouteId);

            if (routeFormId == null) return null;

            var success = Guid.TryParse(routeFormId.ToString(), out Guid formId);

            if (success)
            {
                return await Task.FromResult<Guid?>(formId);
            }

            return await Task.FromResult<Guid?>(null);            
        }
    }
}
