using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;

namespace Unity.GrantManager.Controllers.Authentication.FormSubmission.FormIdResolvers
{
    public class FormIdHeadersResolver : IFormIdResolver
    {
        public async Task<Guid?> ResolvedFormIdAsync(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.Request.Headers.Any())
            {
                return await Task.FromResult<Guid?>(null);
            }

            var formIdKey = AuthConstants.FormIdHeaderId;

            var formIdHeader = context.HttpContext.Request.Headers[formIdKey];
            if (formIdHeader == string.Empty || formIdHeader.Count < 1)
            {
                return await Task.FromResult<Guid?>(null);
            }

            if (formIdHeader.Count > 1)
            {
                Debug.WriteLine(context, $"HTTP request includes more than one {formIdKey} header value. First one will be used. All of them: {formIdHeader.JoinAsString(", ")}");
            }

            var success = Guid.TryParse(formIdHeader[0], out Guid formId);

            if (!success) return await Task.FromResult<Guid?>(null);

            return await Task.FromResult<Guid?>(formId);
        }
    }
}
