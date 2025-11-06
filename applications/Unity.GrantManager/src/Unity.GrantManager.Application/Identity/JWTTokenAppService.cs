using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Unity.Modules.Shared.Permissions;
using Volo.Abp;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;

namespace Unity.GrantManager.Identity
{
    [RemoteService]
    public class JwtTokenAppService : GrantManagerAppService
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly ICurrentUser _currentUser;
        private readonly IConfiguration _configuration;

        public JwtTokenAppService(ICurrentTenant currentTenant, ICurrentUser currentUser, IConfiguration configuration)
        {
            _currentTenant = currentTenant;
            _currentUser = currentUser;
            _configuration = configuration;
        }

        public async Task<string> GenerateJWTTokenAsync()
        {
            // Get user & tenant info
            var userId = _currentUser.GetId().ToString();
            var tenant = _currentTenant.Name ?? "UnknownTenant";
            var isITAdmin = _currentUser.IsInRole(IdentityConsts.ITAdminRoleName);

            // Build claims
            var claims = new[]
            {
                new Claim("user_id", userId),
                new Claim("tenant", tenant),
                new Claim("is_it_admin", isITAdmin.ToString().ToLower()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Ensure secretKey is not null or empty
            var secretKey = _configuration["ReportingAI:JWTSecret"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new AbpException("JWT secret key is not configured.");
            }

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
            return await jwt;
        }
    }
}
