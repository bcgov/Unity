using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Identity;

namespace Unity.GrantManager.Web.Pages.AssigneeSelection
{
    public class AssigneeSelectionModalModel : AbpPageModel
    {
        [BindProperty]
        public Guid AssigneeId { get; set; }
        public List<SelectListItem> AssigneeList { get; set; } = new();

        [BindProperty]
        public string SelectedApplicationIds { get; set; } = default!;

        private readonly IApplicationStatusService _statusService;
        private readonly GrantApplicationAppService _applicationService;
        private readonly IIdentityUserLookupAppService _identityUserLookupAppService;

        public AssigneeSelectionModalModel(IApplicationStatusService statusService,
            GrantApplicationAppService applicationService,
            IIdentityUserLookupAppService identityUserLookupAppService)
        {
            _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
            _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
            _identityUserLookupAppService = identityUserLookupAppService ?? throw new ArgumentNullException(nameof(identityUserLookupAppService));
        }

        public IEnumerable<SelectListItem> GetSelectListItems(ApplicationStatusDto[] statuses)
        {
            return statuses.Select(status => new SelectListItem
            {
                Value = status.Id.ToString(),
                Text = status.InternalStatus.ToString(),
            });
        }

        public async Task OnGetAsync(string applicationIds)
        {
            SelectedApplicationIds = applicationIds;
            AssigneeList ??= new List<SelectListItem>();

            try
            {
                var users = await _identityUserLookupAppService.SearchAsync(new UserLookupSearchInputDto());

                foreach (var user in users.Items.OrderBy(s => s.UserName))
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

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var applicationIds = JsonConvert.DeserializeObject<List<Guid>>(SelectedApplicationIds);

                var selectedUser = await _identityUserLookupAppService.FindByIdAsync(AssigneeId);

                var userName = $"{selectedUser.Name} {selectedUser.Surname}";

                await _applicationService.AddAssignee(applicationIds.ToArray(), AssigneeId.ToString(), userName);

                var statusList = await _statusService.GetListAsync();
                var selectedStatus = statusList.ToList().Find(x => x.StatusCode == ApplicationStatusConsts.SUBMITTED);

                if (selectedStatus != null)
                {
                    await _applicationService.UpdateApplicationStatus(applicationIds.ToArray(), selectedStatus.Id);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "Error updating application status");
            }

            return NoContent();
        }
    }
}
