using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;

namespace Unity.GrantManager.Controllers.Authentication
{
    public class ApiKeyAuthorizationFilter(IConfiguration configuration) : IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(AuthConstants.ApiKeyHeader, out var extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "API Key missing",
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
                });
                return;
            }

            var apiKey = configuration["B2BAuth:ApiKey"];

            if (apiKey is null)
            {
                context.Result = new UnauthorizedObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "API Key not configured",
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
                });
                return;
            }

            if (apiKey != extractedApiKey)
            {
                context.Result = new UnauthorizedObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "Invalid API Key",
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
                });
            }
        }
    }
}
