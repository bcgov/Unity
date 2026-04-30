namespace Unity.AI.Operations;

/// <summary>
/// Execution strategy for multi-step AI flows that iterate over many work items
/// (e.g. multiple attachments, multiple scoresheet sections).
/// </summary>
public enum AIExecutionMode
{
    /// <summary>One item at a time, in order. Default; preserves legacy behavior.</summary>
    Sequential,

    /// <summary>All items started concurrently and awaited together.</summary>
    Parallel,

    /// <summary>Items processed in fixed-size batches, each batch run in parallel.</summary>
    Batch
}
