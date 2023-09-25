using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

public class GetApplicationListDto : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; } = string.Empty;
}
