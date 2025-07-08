using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GlobalTag;

[Serializable]
public class TagUsageSummaryDto : EntityDto<Guid>
{
    public Guid TagId { get; set; }
    public string TagName { get; set; }
    public int ApplicationTagCount { get; set; }
    public int PaymentTagCount { get; set; }
    public int TotalUsageCount => ApplicationTagCount + PaymentTagCount;

}
