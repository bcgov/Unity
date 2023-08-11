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

namespace Unity.GrantManager.Web.Pages.AssigneeSelection
{
    public class AssigneeSelectionModalModel : AbpPageModel
    {
      

        public string statusId { get; set; }

        public List<SelectListItem> statusList { get; set; }
        private readonly IApplicationStatusService _statusService;
    
        public AssigneeSelectionModalModel(IApplicationStatusService statusService)
        {
            _statusService = statusService;

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

            var selectedApplicationIds = JsonSerializer.Deserialize<int[]>(applicationIds);
            statusId = "";
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


    }
}
