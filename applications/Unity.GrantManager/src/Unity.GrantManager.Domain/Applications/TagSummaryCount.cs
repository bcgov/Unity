namespace Unity.GrantManager.Applications;
public class TagSummaryCount(string name, int count)
{
    public string Text { get; set; } = name;
    public int Count { get; set; } = count;
}
