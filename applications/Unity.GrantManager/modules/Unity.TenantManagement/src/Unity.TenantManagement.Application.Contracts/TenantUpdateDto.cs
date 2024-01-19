using Volo.Abp.Domain.Entities;

namespace Unity.TenantManagement;

public class TenantUpdateDto : TenantCreateOrUpdateDtoBase, IHasConcurrencyStamp
{
    public string ConcurrencyStamp { get; set; }
}
