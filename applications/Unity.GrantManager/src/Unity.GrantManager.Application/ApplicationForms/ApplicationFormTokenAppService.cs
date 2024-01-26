using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Encryption;
using System.Linq;
using Unity.GrantManager.Tokens;
using Volo.Abp.MultiTenancy;
using System.Security.Cryptography;

namespace Unity.GrantManager.ApplicationForms
{
    public class ApplicationFormTokenAppService : ApplicationService, IApplicationFormTokenAppService
    {
        private readonly IRepository<TenantToken, Guid> _tenantTokenRepository;
        private readonly IStringEncryptionService _stringEncryptionService;
        private readonly ICurrentTenant _currentTenant;

        public ApplicationFormTokenAppService(IRepository<TenantToken, Guid> tenantTokenRepository,
            IStringEncryptionService stringEncryptionService,
            ICurrentTenant currentTenant)
        {
            _tenantTokenRepository = tenantTokenRepository;
            _stringEncryptionService = stringEncryptionService;
            _currentTenant = currentTenant;
        }

        [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
        public string GenerateFormApiToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        [RemoteService(false)]
        public async Task<string?> GetFormApiTokenAsync()
        {
            if (_currentTenant.Id == null) return null;

            var query = await _tenantTokenRepository.GetQueryableAsync();
            var token = query
                .FirstOrDefault(s => s.TenantId == _currentTenant.Id.Value && s.Name == TokenConsts.IntakeApiName);

            if (token == null) return null;
            if (token.Value != null)
            {
                return _stringEncryptionService.Decrypt(token.Value);
            }
            return null;
        }

        [RemoteService(false)]
        public async Task SetFormApiTokenAsync(string? value)
        {
            if (_currentTenant.Id == null) return;

            var query = await _tenantTokenRepository.GetQueryableAsync();
            var token = query
                .FirstOrDefault(s => s.TenantId == _currentTenant.Id.Value && s.Name == TokenConsts.IntakeApiName);

            if (token == null)
            {
                await _tenantTokenRepository.InsertAsync(new TenantToken()
                {
                    TenantId = _currentTenant.Id.Value,
                    Name = TokenConsts.IntakeApiName,
                    Value = _stringEncryptionService.Encrypt(value)
                });
            }
            else
            {
                token.Value = _stringEncryptionService.Encrypt(value);
                await _tenantTokenRepository.UpdateAsync(token);
            }
        }
    }
}
