using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Applicants;

public class ApplicantListRequestDto : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}