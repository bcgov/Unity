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
using NUglify.Helpers;
using Newtonsoft.Json;

namespace Unity.GrantManager.Web.Pages.StatusUpdate
{
    public class StatusUpdateModalModel : AbpPageModel
    {

        [BindProperty]
        public Guid statusId { get; set; }
        public Guid selectedStatusId { get; set; }

        [TempData]
        public string selectedApplicationIds { get; set; } = "";

        public List<SelectListItem> statusList { get; set; }

        private readonly IApplicationStatusService _statusService;
        private readonly GrantApplicationAppService _applicationService;

        public StatusUpdateModalModel(IApplicationStatusService statusService, GrantApplicationAppService applicationService)
        {
            _statusService = statusService;
            _applicationService = applicationService;

        }

        public async Task OnGetAsync(string applicationIds)
        {
            selectedApplicationIds = applicationIds;

            if (statusList == null)
            {
                statusList = new List<SelectListItem>();
            }

            try
            {

                var status = await _statusService.GetListAsync();

                foreach (var entityDto in status.Items)
                {
                    Console.WriteLine(entityDto.InternalStatus);
                    try
                    {
                        SelectListItem newItem = new SelectListItem
                        {
                            Value = entityDto.Id.ToString(),
                            Text = entityDto.InternalStatus
                        };
                        this.statusList.Add(newItem);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        public async Task<IActionResult> OnPostAsync()
        {

            try
            {

                var applicationIds = JsonConvert.DeserializeObject<List<Guid>>(selectedApplicationIds);

                await _applicationService.UpdateApplicationStatus(applicationIds.ToArray(), statusId);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return NoContent();

        }
    }
}
