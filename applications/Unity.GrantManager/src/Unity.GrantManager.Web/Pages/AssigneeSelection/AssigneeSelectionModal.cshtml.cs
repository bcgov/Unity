using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.Json;
using Keycloak.Net.Models.RealmsAdmin;
using Keycloak.Net;
using Newtonsoft.Json;
using static Volo.Abp.Identity.IdentityPermissions;
using Keycloak.Net.Models.Users;

namespace Unity.GrantManager.Web.Pages.AssigneeSelection
{

    public class AssigneeSelectionModalModel : AbpPageModel
    {

        [BindProperty]
        public string assigneeId { get; set; }
        public List<SelectListItem> assigneeList { get; set; }
        public string realm { get; set; } = "unity";
        [TempData]
        public string selectedApplicationIds { get; set; } = "";
        private readonly IApplicationStatusService _statusService;
        private readonly GrantApplicationAppService _applicationService;
        private readonly KeycloakClient _keycloakClient;

        public AssigneeSelectionModalModel(IApplicationStatusService statusService, GrantApplicationAppService applicationService, KeycloakClient keycloakClient)
        {
            _statusService = statusService;
            _applicationService = applicationService;
            _keycloakClient = keycloakClient;

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

            selectedApplicationIds = applicationIds;

            if (assigneeList == null)
            {
                assigneeList = new List<SelectListItem>();
            }
            try
            {

                IEnumerable<User> assignees = await _keycloakClient.GetUsersAsync(realm).ConfigureAwait(false);

                foreach (User user in assignees)
                {
                    try
                    {
                        SelectListItem newItem = new SelectListItem
                        {
                            Value = user.Id.ToString(),
                            Text = user.FirstName.ToString() + " " + user.LastName.ToString(),
                        };
                        assigneeList.Add(newItem);


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

            }
            catch(Exception ex)
            {
                    Console.WriteLine(ex);
            }

        }

        public async Task<IActionResult> OnPostAsync()
        {
            //TODO: Save the Product...

            try
            {

                var applicationIds = JsonConvert.DeserializeObject<List<Guid>>(selectedApplicationIds);

                User selectedUser = await _keycloakClient.GetUserAsync(realm, assigneeId).ConfigureAwait(false);

                var userName = selectedUser.FirstName.ToString() + " " + selectedUser.LastName.ToString();

                await _applicationService.AddAssignee(applicationIds.ToArray(), assigneeId, userName);

                var statusList = await _statusService.GetListAsync();
                var selectedStatus = statusList.Items.ToList().Find(x => x.StatusCode == "STATUS03");

                await _applicationService.UpdateApplicationStatus(applicationIds.ToArray(), selectedStatus.Id);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return NoContent();

        }

    }
}
