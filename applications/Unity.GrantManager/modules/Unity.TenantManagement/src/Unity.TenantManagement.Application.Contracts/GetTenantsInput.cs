using Volo.Abp.Application.Dtos;

namespace Unity.TenantManagement;

public class GetTenantsInput : PagedAndSortedResultRequestDto
{
    public string Filter { get; set; }
}
