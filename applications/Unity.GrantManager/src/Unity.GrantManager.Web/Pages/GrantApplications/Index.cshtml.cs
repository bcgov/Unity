using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Identity;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

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

        public async Task OnPostAsync() {
            try
            {
                
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "There was an error saving applications.");
            }
        }
    }
}