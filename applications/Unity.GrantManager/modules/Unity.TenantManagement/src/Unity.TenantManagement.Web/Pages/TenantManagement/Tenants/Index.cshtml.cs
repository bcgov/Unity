using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Unity.GrantManager.Integrations;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Tenants;

public class IndexModel : TenantManagementPageModel
{
    protected ICasClientCodeLookupService CasClientCodeLookupService { get; }
    
    public Dictionary<string, string> CasClientCodeHash { get; set; } = new();

    public IndexModel(ICasClientCodeLookupService casClientCodeLookupService)
    {
        CasClientCodeLookupService = casClientCodeLookupService;
    }

    public virtual async Task<IActionResult> OnGetAsync()
    {
        // Create hash where client code is key and code + description is value
        var casClientCodes = await CasClientCodeLookupService.GetActiveOptionsAsync();
        CasClientCodeHash = casClientCodes.ToDictionary(
            c => c.Code, 
            c => c.DisplayName
        );
        
        return Page();
    }

    public virtual Task<IActionResult> OnPostAsync()
    {
        return Task.FromResult<IActionResult>(Page());
    }
}
