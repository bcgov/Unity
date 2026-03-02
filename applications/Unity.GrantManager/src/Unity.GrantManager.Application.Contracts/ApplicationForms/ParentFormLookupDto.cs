using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.ApplicationForms;

[Serializable]
public class ParentFormLookupRequestDto : PagedAndSortedResultRequestDto
{
    public ParentFormLookupRequestDto()
    {
        MaxResultCount = 20;
        Sorting = "ApplicationFormName";
    }

    public string? Filter { get; set; }
    public Guid? ExcludeFormId { get; set; }
}

[Serializable]
public class ParentFormLookupDto
{
    public Guid ApplicationFormId { get; set; }
    public string ApplicationFormName { get; set; } = string.Empty;
    public string? Category { get; set; }
}

