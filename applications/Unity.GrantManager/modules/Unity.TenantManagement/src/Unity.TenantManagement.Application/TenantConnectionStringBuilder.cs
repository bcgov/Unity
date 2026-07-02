using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Unity.TenantManagement.Application.Contracts;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.TenantManagement;

namespace Unity.TenantManagement.Application
{
    [RemoteService(false)]
    public class TenantConnectionStringBuilder : ApplicationService, ITenantConnectionStringBuilder
    {
        private static readonly char[] Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static readonly char[] Digits = "0123456789".ToCharArray();

        // Deliberately excludes quote/backslash characters — the password is interpolated into
        // a single-quoted SQL literal by EntityFrameworkCoreGrantManagerDbSchemaMigrator, so
        // restricting the charset here removes that injection surface at the source rather than
        // relying solely on escaping at the consuming end.
        private static readonly char[] PasswordAlphabet =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

        private readonly IConfiguration _configuration;
        private readonly ITenantRepository _tenantRepository;

        public TenantConnectionStringBuilder(IConfiguration configuration, ITenantRepository tenantRepository)
        {
            _configuration = configuration;
            _tenantRepository = tenantRepository;
        }

        public string Build(string tenantName, TenantDbCredentials credentials)
        {
            var baseConnectionString = _configuration.GetConnectionString(UnityTenantManagementConsts.TenantConnectionStringName)
                ?? throw new UserFriendlyException("Connection string configuration error");

            return ReplaceKeyValues(baseConnectionString, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Database"] = credentials.DbName,
                ["Username"] = credentials.Username,
                ["Password"] = credentials.Password
            });
        }

        // Replaces connection string values by key (case-insensitive) while preserving
        // the original key casing from the template (e.g. "Host" stays "Host", not "host").
        private static string ReplaceKeyValues(string connectionString, Dictionary<string, string> replacements)
        {
            var parts = connectionString.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                var eq = parts[i].IndexOf('=');
                if (eq > 0 && replacements.TryGetValue(parts[i][..eq].Trim(), out var newValue))
                {
                    parts[i] = $"{parts[i][..eq]}={newValue}";
                }
            }
            return string.Join(";", parts);
        }

        public async Task<TenantDbCredentials> GenerateCredentialsAsync()
        {
            var allTenants = await _tenantRepository.GetListAsync(nameof(Tenant.Name), int.MaxValue, 0, null, includeDetails: true);

            var existingDbNames = allTenants
                .Where(t => t.ExtraProperties.ContainsKey(UnityTenantManagementConsts.TenantLicencePlateExtraPropertyKey))
                .Select(t => t.ExtraProperties[UnityTenantManagementConsts.TenantLicencePlateExtraPropertyKey]?.ToString() ?? "")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            string dbName;
            do
            {
                dbName = GenerateDbName();
            }
            while (existingDbNames.Contains(dbName));

            return new TenantDbCredentials(dbName, dbName, GeneratePassword());
        }

        public TenantDbCredentials GenerateReadOnlyCredentials(TenantDbCredentials credentials)
        {
            return new TenantDbCredentials(credentials.DbName, $"{credentials.Username}_readonly", GeneratePassword());
        }

        private static string GenerateDbName()
        {
            // Format: T_XXX999 where X is A-Z and 9 is 0-9, e.g. T_ABC123
            Span<char> chars =
            [
                'T',
                '_',
                Letters[RandomNumberGenerator.GetInt32(Letters.Length)],
                Letters[RandomNumberGenerator.GetInt32(Letters.Length)],
                Letters[RandomNumberGenerator.GetInt32(Letters.Length)],
                Digits[RandomNumberGenerator.GetInt32(Digits.Length)],
                Digits[RandomNumberGenerator.GetInt32(Digits.Length)],
                Digits[RandomNumberGenerator.GetInt32(Digits.Length)]
            ];
            return new string(chars);
        }

        private static string GeneratePassword()
        {
            // 24 cryptographically random characters (mixed-case letters + digits) — generated
            // via RandomNumberGenerator (CSPRNG), not the non-cryptographic Random.Shared, since
            // this protects a live PostgreSQL login role's credentials.
            const int length = 24;
            Span<char> chars = new char[length];
            for (var i = 0; i < length; i++)
            {
                chars[i] = PasswordAlphabet[RandomNumberGenerator.GetInt32(PasswordAlphabet.Length)];
            }
            return new string(chars);
        }
    }
}
