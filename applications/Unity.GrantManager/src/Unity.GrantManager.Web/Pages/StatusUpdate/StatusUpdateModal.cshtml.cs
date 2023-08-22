using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace Unity.GrantManager.Web.Pages.StatusUpdate
{
    public class StatusUpdateModalModel : AbpPageModel
    {
        [BindProperty]
        public Guid statusId { get; set; }
        public Guid selectedStatusId { get; set; }

        [BindProperty]
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

            statusList ??= new List<SelectListItem>();

            try
            {
                var statuses = await _statusService.GetListAsync();

                foreach (var entityDto in statuses)
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
                Logger.LogError(ex, "Error getting application statuses");
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
                Logger.LogError(ex, "Error updating application statuses");
            }
            return NoContent();
        }
    }
}
