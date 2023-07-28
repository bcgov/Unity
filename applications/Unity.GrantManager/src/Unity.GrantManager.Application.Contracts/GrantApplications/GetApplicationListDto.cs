using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

public class GetApplicationListDto : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }
}
