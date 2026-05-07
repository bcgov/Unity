using System;

namespace Unity.GrantManager.Applicants;

public class HandleSupplierAfterMergeDto
{
    public Guid PrincipalId { get; set; }
    public Guid NonPrincipalId { get; set; }
    public Guid? SelectedSupplierId { get; set; }
}
