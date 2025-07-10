using Unity.GrantManager.GlobalTag;

namespace Unity.Payments.Domain.PaymentTags;
public class PaymentTagSummaryCount(Tag tag, int count)
{
    public Tag Tag { get; set; } = tag;
    public int Count { get; set; } = count;
}
