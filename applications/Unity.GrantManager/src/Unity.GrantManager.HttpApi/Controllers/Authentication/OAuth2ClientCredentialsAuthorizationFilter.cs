using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace Unity.GrantManager.Controllers.Authentication
{
    /// <summary>
    /// Authorization filter for OAuth 2.0 Client Credentials flow.
    /// Validates JWT tokens issued by Keycloak for B2B/M2M authentication.
    /// </summary>
    public class OAuth2ClientCredentialsAuthorizationFilter(IConfiguration configuration) : IAsyncAuthorizationFilter
    {        
        private readonly JwtSecurityTokenHandler _tokenHandler = new();

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Extract the Authorization header
            if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "missing_token", error_description = "Missing Authorization header" });
                return;
            }

            var authHeaderValue = authHeader.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeaderValue) || !authHeaderValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "invalid_token", error_description = "Invalid Authorization header format. Expected: Bearer <token>" });
                return;
            }

            // Extract the token
            var token = authHeaderValue.Substring("Bearer ".Length).Trim();

            try
            {
                // Validate the token
                var claimsPrincipal = await ValidateTokenAsync(token);

                if (claimsPrincipal == null)
                {
                    context.Result = new UnauthorizedObjectResult(new { error = "invalid_token", error_description = "Token validation failed" });
                    return;
                }

                // Optional: Add additional claims validation
                var audience = claimsPrincipal.FindFirst("aud")?.Value;
                var clientId = claimsPrincipal.FindFirst("azp")?.Value ?? claimsPrincipal.FindFirst("client_id")?.Value;
                
                var expectedAudience = configuration["B2BOAuth:Audience"];
                if (!string.IsNullOrEmpty(expectedAudience) && audience != expectedAudience)
                {
                    context.Result = new UnauthorizedObjectResult(new { error = "invalid_token", error_description = "Invalid audience" });
                    return;
                }

                // Optional: Validate specific client IDs if configured
                var allowedClientIds = configuration["B2BOAuth:AllowedClientIds"];
                if (!string.IsNullOrEmpty(allowedClientIds))
                {
                    var allowedClients = allowedClientIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
                        .ToList();

                    if (string.IsNullOrEmpty(clientId) || !allowedClients.Contains(clientId))
                    {
                        context.Result = new ForbidResult();
                        return;
                    }
                }

                // Store the principal for use in the controller if needed
                context.HttpContext.User = claimsPrincipal;

                // Authentication successful
            }
            catch (SecurityTokenExpiredException)
            {
                context.Result = new UnauthorizedObjectResult(new { error = "invalid_token", error_description = "Token has expired" });
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                context.Result = new UnauthorizedObjectResult(new { error = "invalid_token", error_description = "Invalid token signature" });
            }
            catch (SecurityTokenValidationException ex)
            {
                context.Result = new UnauthorizedObjectResult(new { error = "invalid_token", error_description = ex.Message });
            }
            catch (Exception)
            {
                context.Result = new UnauthorizedObjectResult(new { error = "invalid_token", error_description = "Token validation failed" });
            }
        }

        private async Task<System.Security.Claims.ClaimsPrincipal?> ValidateTokenAsync(string token)
        {
            var serverAddress = configuration["AuthServer:ServerAddress"];
            var realm = configuration["AuthServer:Realm"];

            if (string.IsNullOrEmpty(serverAddress) || string.IsNullOrEmpty(realm))
            {
                throw new InvalidOperationException("AuthServer configuration is missing");
            }

            // Construct the issuer URL for Keycloak
            var issuer = $"{serverAddress}/realms/{realm}";

            // Get JWKS endpoint for token validation
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{issuer}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever());

            var openIdConfig = await configurationManager.GetConfigurationAsync();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = false, // Client credentials tokens may not have audience
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = openIdConfig.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(2) // Allow 2 minutes clock skew
            };

            var claimsPrincipal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            return claimsPrincipal;
        }
    }
}
