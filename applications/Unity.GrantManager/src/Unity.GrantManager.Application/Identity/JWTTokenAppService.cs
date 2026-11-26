using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Permissions;
using Unity.AI.Runtime.Execution;
using Unity.AI.Settings;
using Unity.GrantManager.Integrations.Metabase;
using Unity.Modules.Shared.Permissions;
using Volo.Abp;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;
using Volo.Abp.Users;

namespace Unity.GrantManager.Identity
{
    [RemoteService]
    public class JwtTokenAppService : GrantManagerAppService
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly ICurrentUser _currentUser;
        private readonly IConfiguration _configuration;
        private readonly IPermissionChecker _permissionChecker;
        private readonly ITenantRepository _tenantRepository;
        private readonly ISettingProvider _settingProvider;
        private readonly AIReportingModelResolver _reportingModelResolver;

        public JwtTokenAppService(
            ICurrentTenant currentTenant,
            ICurrentUser currentUser,
            IConfiguration configuration,
            IPermissionChecker permissionChecker,
            ITenantRepository tenantRepository,
            ISettingProvider settingProvider,
            AIReportingModelResolver reportingModelResolver)
        {
            _reportingModelResolver = reportingModelResolver;
            _currentTenant = currentTenant;
            _currentUser = currentUser;
            _configuration = configuration;
            _permissionChecker = permissionChecker;
            _tenantRepository = tenantRepository;
            _settingProvider = settingProvider;
        }

        public async Task<string> GenerateJWTTokenAsync()
        {
            // Get user & tenant info
            var userId = _currentUser.GetId().ToString();
            var tenant = _currentTenant.Name ?? "UnknownTenant";
            var isITAdmin = _currentUser.IsInRole(IdentityConsts.ITAdminRoleName);
            var canEditDataModel = await _permissionChecker.IsGrantedAsync(
                AIPermissions.Reporting.CreateEditDataModel);

            // UAI reads the tenant's Metabase database and collection from these claims
            // (AB#33978), so no token is issued for a tenant without them.
            var (databaseId, collectionId) = await GetMetabaseIdsAsync();
            var schemaTypes = await GetSchemaTypesAsync();

            // Build claims
            var claims = new List<Claim>
            {
                new Claim("user_id", userId),
                new Claim("tenant", tenant),
                new Claim("is_it_admin", isITAdmin.ToString().ToLower()),
                new Claim("can_edit_data_model", canEditDataModel.ToString().ToLower()),
                new Claim("mb_db_id", databaseId.ToString(CultureInfo.InvariantCulture)),
                new Claim("mb_collection_id", collectionId.ToString(CultureInfo.InvariantCulture)),
                new Claim("schema_types", schemaTypes),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // AI Reporting's models, each with its API version, come only from AI Setup (AB#35030):
            // UAI has no defaults, so no token is issued until both models are set.
            var reportingModels = await _reportingModelResolver.ResolveAsync();
            if (!reportingModels.IsComplete)
            {
                throw new BusinessException(AIErrorCodes.ReportingModelsNotSet);
            }

            claims.Add(new Claim("ai_chat_deployment", reportingModels.ChatDeployment!));
            claims.Add(new Claim("ai_chat_api_version", reportingModels.ChatApiVersion!));
            claims.Add(new Claim("ai_embedding_deployment", reportingModels.EmbeddingDeployment!));
            claims.Add(new Claim("ai_embedding_api_version", reportingModels.EmbeddingApiVersion!));

            // Ensure secretKey is not null or empty
            var secretKey = _configuration["ReportingAI:JWTSecret"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new AbpException("JWT secret key is not configured.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
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
            return jwt;
        }

        private async Task<(int DatabaseId, int CollectionId)> GetMetabaseIdsAsync()
        {
            if (_currentTenant.Id is not Guid tenantId)
            {
                throw new BusinessException(AIErrorCodes.ReportingNotSetUp);
            }

            Tenant? tenant;
            using (_currentTenant.Change(null))
            {
                tenant = await _tenantRepository.FindAsync(tenantId, includeDetails: false);
            }

            var databaseId = tenant?.GetProperty<int?>(MetabaseTenantProvisioner.DatabaseIdProperty);
            var collectionId = tenant?.GetProperty<int?>(MetabaseTenantProvisioner.CollectionIdProperty);
            if (databaseId is not int db || collectionId is not int collection)
            {
                throw new BusinessException(AIErrorCodes.ReportingNotSetUp);
            }

            return (db, collection);
        }

        private async Task<string> GetSchemaTypesAsync()
        {
            var value = await _settingProvider.GetOrNullAsync(AISettings.ReportingSchemaTypes);
            var schemaTypes = (value ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(type => type.ToLowerInvariant())
                .Distinct()
                .ToArray();

            return schemaTypes.Length == 0
                ? AISettings.DefaultReportingSchemaTypes
                : string.Join(",", schemaTypes);
        }
    }
}
