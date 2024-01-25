using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Unity.TenantManagement.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Handlers
{
    public class TenantManagerAssignmentEventHandler
        : ILocalEventHandler<TenantAssignManagerEto>, ITransientDependency
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly IUserImportAppService _userImportAppService;

        public TenantManagerAssignmentEventHandler(ICurrentTenant currentTenant,
            IUserImportAppService userImportAppService)
        {
            _currentTenant = currentTenant;
            _userImportAppService = userImportAppService;
        }

        public async Task HandleEventAsync(TenantAssignManagerEto tenantProgramManagerCreatedEto)
        {
            using (_currentTenant.Change(tenantProgramManagerCreatedEto.TenantId))
            {
                await _userImportAppService.ImportUserAsync(new ImportUserDto()
                {
                    Directory = "IDIR",
                    Guid = tenantProgramManagerCreatedEto.UserIdentifier,
                    Roles = new string[] { UnityRoles.ProgramManager }
                });
            }
        }
    }
}
