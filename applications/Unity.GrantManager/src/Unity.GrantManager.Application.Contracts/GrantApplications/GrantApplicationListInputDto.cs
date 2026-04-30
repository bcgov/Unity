using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications
{
    public class GrantApplicationListInputDto : PagedAndSortedResultRequestDto
    {
        public DateTime? SubmittedFromDate { get; set; }
        public DateTime? SubmittedToDate { get; set; }

        /// <summary>
        /// Column names that are currently visible in the UI. When provided, only 
        /// the database JOINs required to populate those columns are executed. 
        /// Pass null or an empty list to load everything (backward-compatible).
        /// </summary>
        public List<string>? RequestedFields { get; set; }
    }
}
