using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unity.GrantManager.Controllers.Authentication
{
    public class BasicAuthenticationAuthorizationFilter(IConfiguration configuration) : IAsyncAuthorizationFilter
    {        
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Extract the Authorization header
            if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Missing Authorization header" });
                return Task.CompletedTask;
            }

            var authHeaderValue = authHeader.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeaderValue) || !authHeaderValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Invalid Authorization header format" });
                return Task.CompletedTask;
            }

            // Decode the base64-encoded credentials
            try
            {
                var encodedCredentials = authHeaderValue.Substring("Basic ".Length).Trim();
                var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                var credentials = decodedCredentials.Split(':', 2);

                if (credentials.Length != 2)
                {
                    context.Result = new UnauthorizedObjectResult(new { error = "Invalid credentials format" });
                    return Task.CompletedTask;
                }

                var username = credentials[0];
                var password = credentials[1];

                // Validate credentials against configuration
                var configuredUsername = configuration["B2BAuth:Username"];
                var configuredPassword = configuration["B2BAuth:Password"];

                if (string.IsNullOrEmpty(configuredUsername) || string.IsNullOrEmpty(configuredPassword))
                {
                    context.Result = new StatusCodeResult(500); // Internal server error - configuration missing
                    return Task.CompletedTask;
                }

                if (username != configuredUsername || password != configuredPassword)
                {
                    context.Result = new UnauthorizedObjectResult(new { error = "Invalid credentials" });
                    return Task.CompletedTask;
                }

                // Authentication successful
                return Task.CompletedTask;
            }
            catch (FormatException)
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Invalid Authorization header encoding" });
                return Task.CompletedTask;
            }
            catch (Exception)
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Authentication failed" });
                return Task.CompletedTask;
            }
        }
    }
}
