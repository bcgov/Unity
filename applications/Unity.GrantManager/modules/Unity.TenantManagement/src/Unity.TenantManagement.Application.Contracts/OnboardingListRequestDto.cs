#nullable enable
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.TenantManagement;

public class OnboardingListRequestDto : PagedAndSortedResultRequestDto
{
    public string? Category { get; set; } = "Onboarding";
    public string? Filter { get; set; }
    public List<ColumnFilterDto>? ColumnFilters { get; set; }
}
