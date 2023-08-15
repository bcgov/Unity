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

namespace Unity.GrantManager.Web.Pages.StatusUpdate
{
    public class StatusUpdateModalModel : AbpPageModel
    {
      

        public Guid statusId { get; set; }
        public Guid[] selectedApplicationIds { get; set; }

        public List<SelectListItem> statusList { get; set; }



        private readonly IApplicationStatusService _statusService;
        private readonly GrantApplicationAppService _applicationService;

        public StatusUpdateModalModel(IApplicationStatusService statusService, GrantApplicationAppService applicationService)
        {
            _statusService = statusService;
            _applicationService = applicationService;

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

            selectedApplicationIds = JsonSerializer.Deserialize<Guid[]>(applicationIds);
           
            if (statusList == null)
            {
                statusList = new List<SelectListItem>();
            }
            var status = await _statusService.GetListAsync();
            Console.WriteLine("Status " + status.Items);
            Console.WriteLine("applicatioId" + selectedApplicationIds.Length.ToString());
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

        public async Task<IActionResult> OnPostAsync()
        {
            //TODO: Save the Product...

            Console.WriteLine("Submitted");
            var result = _applicationService.UpdateApplicationStatus(selectedApplicationIds, statusId);
            Console.WriteLine("Update status result ------" + result.ToString());


            return NoContent();
        }
    }
}
