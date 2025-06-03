using System.Collections.Generic;

namespace Unity.Modules.Shared;

public class PartialUpdateDto<T> where T : class
{
    public List<string> ModifiedFields { get; set; } = new();

    public required T Data { get; set; }
}
