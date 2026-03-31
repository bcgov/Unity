using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Identity;

using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Web.Pages.Administration
{
    [Authorize]
    public class IndexModel(IUserTenantAppService userTenantAppService, ICurrentTenant currentTenant) : GrantManagerPageModel
    {
        public List<string> Tenants { get; set; } = [];

        public string CurrentSelectedTenant { get; set; } = currentTenant?.Name ?? string.Empty;

        public bool IsRestricted { get; set; } = true;

        public async Task OnGetAsync()
        {
            IsRestricted = !string.IsNullOrEmpty(CurrentSelectedTenant);
            var userTenants = await userTenantAppService.GetListAsync();
            Tenants = userTenants
                .Where(t => t.TenantName != null)
                .Select(t => t.TenantName!)
                .ToList();
        }
    }
}
