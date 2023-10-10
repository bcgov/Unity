using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Volo.Abp.Identity;

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

        private readonly IdentityUserStore _identityUserStore;
        private readonly ILookupNormalizer _lookupNormalizer;

        public IndexModel(
            IdentityUserStore identityUserStore,
            ILookupNormalizer lookupNormalizer)
        {
            _identityUserStore = identityUserStore;
            _lookupNormalizer = lookupNormalizer;
        }

        public async Task OnGetAsync()
        {
            try
            {
                var users = await _identityUserStore
                    .GetUsersInRoleAsync(_lookupNormalizer.NormalizeName(UnityRoles.Reviewer));
                AssigneeList ??= new List<SelectListItem>();
                foreach (var user in users.OrderBy(s => s.Surname).ThenBy(s => s.Name))
                {
                    AssigneeList.Add(new()
                    {
                        Value = user.Id.ToString(),
                        Text = $"{user.Surname}, {user.Name}",
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