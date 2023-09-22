using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Identity;

namespace Unity.GrantManager.Web.Pages.GrantApplications
{
    [Authorize]
    public class IndexModel : GrantManagerPageModel
    {
        [BindProperty(SupportsGet = true)]
        public Guid? FormId { get; set; }

        public IReadOnlyList<IdentityUserDto> Users { get; set; } = new List<IdentityUserDto>();

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