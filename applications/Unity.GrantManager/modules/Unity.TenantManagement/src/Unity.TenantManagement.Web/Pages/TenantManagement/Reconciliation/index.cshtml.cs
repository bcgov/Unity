using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Reconciliation
{
    [Authorize] 
    public class IndexModel : ReconciliationPageModel
    {

        [BindProperty]
        public DateTime? SubmissionDateFrom { get; set; }
        [BindProperty]
        public DateTime? SubmissionDateTo { get; set; }

        public void OnGet()
        { // Initialize data or view logic here 
        }
    }
}