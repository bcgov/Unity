using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;

namespace Unity.GrantManager.Controllers.Authentication.FormSubmission.FormIdResolvers
{
    public class FormIdQueryStringResolver : IFormIdResolver
    {
        public async Task<Guid?> ResolvedFormIdAsync(AuthorizationFilterContext context)
        {
            if (context.HttpContext.Request.QueryString.HasValue)
            {
                var formIdKey = AuthConstants.FormIdQueryStringId;
                if (context.HttpContext.Request.Query.ContainsKey(formIdKey))
                {
                    var formIdValue = context.HttpContext.Request.Query[formIdKey].ToString();
                    if (formIdKey.IsNullOrWhiteSpace())
                    {
                        return await Task.FromResult<Guid?>(null);
                    }

                    var success = Guid.TryParse(formIdValue, out Guid formId);

                    if (!success) { return await Task.FromResult<Guid?>(null); }

                    return formId;
                }
            }

            return null;
        }
    }
}
