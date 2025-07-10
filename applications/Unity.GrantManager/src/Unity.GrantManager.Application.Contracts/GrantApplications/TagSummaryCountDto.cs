using Unity.GrantManager.GlobalTag;
namespace Unity.GrantManager.GrantApplications;
public class TagSummaryCountDto
{
    public required TagDto Tag { get; set; }
    public required int Count { get; set; }
}
