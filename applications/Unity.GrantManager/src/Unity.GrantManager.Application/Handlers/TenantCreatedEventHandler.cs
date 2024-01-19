using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Data;
using Unity.GrantManager.Identity;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Handlers
{
    public class TenantCreatedEventHandler
        : ILocalEventHandler<TenantCreatedEto>, ITransientDependency
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly IUserImportAppService _userImportAppService;

        private readonly GrantManagerDbMigrationService _grantManagerDbMigrationService;

        public TenantCreatedEventHandler(ITenantRepository tenantRepository,
            ICurrentTenant currentTenant,
            IUserImportAppService userImportAppService,
            GrantManagerDbMigrationService grantManagerDbMigrationService)
        {
            _tenantRepository = tenantRepository;
            _grantManagerDbMigrationService = grantManagerDbMigrationService;
            _currentTenant = currentTenant;
            _userImportAppService = userImportAppService;
        }

        public async Task HandleEventAsync(TenantCreatedEto tenantCreatedEto)
        {
            var tenant = await _tenantRepository.GetAsync(tenantCreatedEto.Id);
            var userIdentifier = tenantCreatedEto.Properties["UserIdentifier"];

            await _grantManagerDbMigrationService
                .MigrateAndSeedTenantAsync(new HashSet<string>(), tenant);

            using (_currentTenant.Change(tenant.Id))
            {
                await _userImportAppService.ImportUserAsync(new ImportUserDto()
                { Directory = "IDIR", Guid = userIdentifier, Roles = new string[] { UnityRoles.ProgramManager } });
            }
        }
    }
}
