using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;

namespace Unity.GrantManager.Controllers.Authentication.FormSubmission.FormIdResolvers
{
    public class FormIdRequestBodyResolver : IFormIdResolver
    {
        public async Task<Guid?> ResolvedFormIdAsync(AuthorizationFilterContext context)
        {
            var body = await GetRawBodyAsync(context.HttpContext.Request);

            JObject obj = JObject.Parse(body);

            if (obj == null) return await Task.FromResult<Guid?>(null);

            var token = obj![AuthConstants.FormIdRequestBodyId];

            if (token == null) return await Task.FromResult<Guid?>(null);

            var success = Guid.TryParse(token.ToString(), out Guid formId);

            if (success)
            {
                return await Task.FromResult<Guid?>(formId);
            }

            return await Task.FromResult<Guid?>(null);
        }

        public static async Task<string> GetRawBodyAsync(HttpRequest request, Encoding? encoding = default)
        {
            if (!request.Body.CanSeek)
            {
                // We only do this if the stream isn't *already* seekable,
                // as EnableBuffering will create a new stream instance
                // each time it's called
                request.EnableBuffering();
            }

            request.Body.Position = 0;

            var reader = new StreamReader(request.Body, encoding ?? Encoding.UTF8);

            var body = await reader.ReadToEndAsync().ConfigureAwait(false);

            request.Body.Position = 0;

            return body;
        }
    }
}
