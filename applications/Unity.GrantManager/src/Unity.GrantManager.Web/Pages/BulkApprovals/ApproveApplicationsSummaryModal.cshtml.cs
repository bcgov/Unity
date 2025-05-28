using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Pages.BulkApprovals
{
    public class ApproveApplicationsSummaryModalModel : PageModel
    {
        [BindProperty]
        public List<BulkApprovalItemResult>? BulkApprovalResults { get; set; }

        public void OnGet(string summaryJson)
        {
            var items = new List<BulkApprovalItemResult>();

            var result = JsonSerializer.Deserialize<BulkApprovalResultDto>(summaryJson);

            foreach (var item in result?.Successes ?? [])
            {
                items.Add(new BulkApprovalItemResult
                {
                    ReferenceNo = item,
                    Message = "Success",
                    IsSuccess = true
                });
            }

            foreach (var item in result?.Failures ?? [])
            {
                items.Add(new BulkApprovalItemResult
                {
                    ReferenceNo = item.Key,
                    Message = item.Value,
                    IsSuccess = false
                });
            }

            BulkApprovalResults = [.. items.OrderBy(s => s.ReferenceNo)];
        }

        public class BulkApprovalItemResult
        {
            public string ReferenceNo { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public bool IsSuccess { get; set; }
        }
    }
}
