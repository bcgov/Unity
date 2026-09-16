using System.Collections.Generic;

namespace Unity.GrantManager.Applications;

public class FiscalYearEndRolloverResult
{
    public int RowsAffected { get; init; }
    public IReadOnlyList<string> MissingColumns { get; init; } = [];
    public bool ColumnsAvailable => MissingColumns.Count == 0;
}
