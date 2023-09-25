using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.StatusUpdate
{
    public class StatusUpdateModalModel : AbpPageModel
    {
        [BindProperty]
        public Guid StatusId { get; set; }
        public Guid SelectedStatusId { get; set; }

        [BindProperty]
        public string SelectedApplicationIds { get; set; } = string.Empty;

        public List<SelectListItem> StatusList { get; set; } = new();

        private readonly IApplicationStatusService _statusService;
        private readonly GrantApplicationAppService _applicationService;

        public StatusUpdateModalModel(IApplicationStatusService statusService, GrantApplicationAppService applicationService)
        {
            _statusService = statusService;
            _applicationService = applicationService;
        }

        public async Task OnGetAsync(string applicationIds)
        {
            SelectedApplicationIds = applicationIds;

            StatusList ??= new List<SelectListItem>();

            try
            {
                var statuses = await _statusService.GetListAsync();

                foreach (var entityDto in statuses)
                {
                    Console.WriteLine(entityDto.InternalStatus);
                    try
                    {
                        SelectListItem newItem = new()
                        {
                            Value = entityDto.Id.ToString(),
                            Text = entityDto.InternalStatus
                        };
                        this.StatusList.Add(newItem);

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
            var applicationIds = JsonConvert.DeserializeObject<List<Guid>>(SelectedApplicationIds);
            if (null != applicationIds)
            {
                await _applicationService.UpdateApplicationStatus(applicationIds.ToArray(), StatusId);
            }
            return NoContent();
        }
    }
}
