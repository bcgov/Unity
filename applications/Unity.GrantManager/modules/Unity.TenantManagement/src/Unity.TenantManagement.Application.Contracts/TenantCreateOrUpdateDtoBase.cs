using System.ComponentModel.DataAnnotations;
using Volo.Abp.ObjectExtending;
using Volo.Abp.TenantManagement;
using Volo.Abp.Validation;

namespace Unity.TenantManagement;

public abstract class TenantCreateOrUpdateDtoBase : ExtensibleObject
{
    [Required]
    [DynamicStringLength(typeof(TenantConsts), nameof(TenantConsts.MaxNameLength))]
    [Display(Name = "TenantName")]
    public string Name { get; set; }

    public string Division { get; set; } = string.Empty;
    public string Branch { get; set; }  = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CasClientCode { get; set; } = string.Empty;

    protected TenantCreateOrUpdateDtoBase() : base(false)
    {
    }
}
