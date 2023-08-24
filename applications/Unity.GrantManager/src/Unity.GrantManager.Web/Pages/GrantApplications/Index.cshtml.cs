using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Identity;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Unity.GrantManager.Web.Pages.GrantApplications
{
    [Authorize]
    public class IndexModel : GrantManagerPageModel
    {
        [BindProperty(SupportsGet = true)]
        public Guid? FormId { get; set; }

        public IReadOnlyList<IdentityUserDto> Users { get; set; } = default!;
                
        private readonly IIdentityUserLookupAppService _identityUserLookupAppService;

        public IndexModel(IIdentityUserLookupAppService identityUserLookupAppService)
        {
            _identityUserLookupAppService = identityUserLookupAppService ?? throw new ArgumentNullException(nameof(identityUserLookupAppService));                        
        }
        
        public async Task OnGetAsync()
        {
            try
            {
                var users = (await _identityUserLookupAppService.SearchAsync(new UserLookupSearchInputDto())).Items;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "Error loading users select list");
            }
        }
    }
}