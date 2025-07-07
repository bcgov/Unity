
using Unity.GrantManager.GlobalTag;

namespace Unity.GrantManager.Applications;


public class TagSummaryCount(Tag tag, int count)
{
    public Tag Tag { get; set; } = tag;
    public int Count { get; set; } = count;
}
