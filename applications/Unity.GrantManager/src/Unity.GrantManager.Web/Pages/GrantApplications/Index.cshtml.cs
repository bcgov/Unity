using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Integration;

namespace Unity.GrantManager.Web.Pages.GrantApplications
{
    [Authorize]
    public class IndexModel : GrantManagerPageModel
    {

        [BindProperty]
        public Guid AssigneeId { get; set; }
        public List<SelectListItem> AssigneeList { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public Guid? FormId { get; set; }

        public IReadOnlyList<IdentityUserDto> Users { get; set; } = new List<IdentityUserDto>();

        private readonly IIdentityUserIntegrationService _identityUserLookupAppService;

        public IndexModel(IIdentityUserIntegrationService identityUserLookupAppService)
        {
            _identityUserLookupAppService = identityUserLookupAppService ?? throw new ArgumentNullException(nameof(identityUserLookupAppService));
        }

        public async Task OnGetAsync()
        {
            try
            {
                var users = (await _identityUserLookupAppService.SearchAsync(new UserLookupSearchInputDto())).Items;
                AssigneeList ??= new List<SelectListItem>();
                foreach (var user in users.OrderBy(s => s.UserName))
                {
                    AssigneeList.Add(new()
                    {
                        Value = user.Id.ToString(),
                        Text = $"{user.Name} {user.Surname}",
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "Error loading users select list");
            }
        }
    }
}