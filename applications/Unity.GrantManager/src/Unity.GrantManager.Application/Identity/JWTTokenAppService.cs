using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;

namespace Unity.GrantManager.Identity
{
    [RemoteService]
    public class JWTTokenAppService : GrantManagerAppService
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly ICurrentUser _currentUser;

        public JWTTokenAppService(ICurrentTenant currentTenant, ICurrentUser currentUser)
        {
            _currentTenant = currentTenant;
            _currentUser = currentUser;
        }

        public async Task<string> GenerateJWTTokenAsync()
        {
            // Get user & tenant info
            var userId = _currentUser.GetId().ToString();
            var tenant = _currentTenant.Name ?? "UnknownTenant";

            // Build claims
            var claims = new[]
            {
                new Claim("user_id", userId ?? "unknown"),
                new Claim("tenant", tenant),
                new Claim("mb_url", "https://test-unity-reporting.apps.silver.devops.gov.bc.ca"), // TODO: change based on env in OpenShift
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Secret key (keep secure in production, e.g., in appsettings or Azure Key Vault)
            var secretKey = "INSERT SUPER SECRET KEY HERE FILLER FILLER"; // TODO: replace with secure key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create token
            var token = new JwtSecurityToken(
                issuer: "Unity.GrantManager",
                audience: "Unity.GrantManager.Users",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            // Return encoded JWT
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return await Task.FromResult(jwt);
        }
    }
}