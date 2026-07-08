namespace Unity.AI.Operations;

/// <summary>
/// Execution strategy for multi-step AI flows that iterate over many work items
/// (e.g. multiple attachments, multiple scoresheet sections).
/// </summary>
public enum AIExecutionMode
{
    /// <summary>Single request with no item fan-out.</summary>
    Single,

    /// <summary>One item at a time, in order. Default; preserves legacy behavior.</summary>
    Sequential,

    /// <summary>All items started concurrently and awaited together.</summary>
    Parallel,

    /// <summary>All items sent through one batch operation.</summary>
    Batch
}
