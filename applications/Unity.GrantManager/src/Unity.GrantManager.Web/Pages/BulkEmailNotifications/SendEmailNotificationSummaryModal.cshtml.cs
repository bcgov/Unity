using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Pages.BulkEmailNotifications
{
    public class SendEmailNotificationSummaryModalModel : PageModel
    {
        [BindProperty]
        public List<BulkEmailNotificationItemResult>? BulkEmailNotificationResults { get; set; }

        public void OnGet(string summaryJson)
        {
            var items = new List<BulkEmailNotificationItemResult>();

            var result = JsonSerializer.Deserialize<BulkEmailNotificationResultDto>(summaryJson);

            foreach (var item in result?.Successes ?? [])
            {
                items.Add(new BulkEmailNotificationItemResult
                {
                    ReferenceNo = item,
                    Message = "Queued for delivery",
                    IsSuccess = true
                });
            }

            foreach (var item in result?.Failures ?? [])
            {
                items.Add(new BulkEmailNotificationItemResult
                {
                    ReferenceNo = item.Key,
                    Message = item.Value,
                    IsSuccess = false
                });
            }

            BulkEmailNotificationResults = [.. items.OrderBy(s => s.ReferenceNo)];
        }

        public class BulkEmailNotificationItemResult
        {
            public string ReferenceNo { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public bool IsSuccess { get; set; }
        }
    }
}
