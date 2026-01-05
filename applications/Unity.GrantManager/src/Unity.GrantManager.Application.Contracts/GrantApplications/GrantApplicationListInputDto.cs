using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications
{
    public class GrantApplicationListInputDto : PagedAndSortedResultRequestDto
    {
        public DateTime? SubmittedFromDate { get; set; }
        public DateTime? SubmittedToDate { get; set; }
    }
}
