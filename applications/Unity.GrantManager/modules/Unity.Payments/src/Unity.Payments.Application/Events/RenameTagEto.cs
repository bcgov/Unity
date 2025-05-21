using System;

namespace Unity.Payments.Events;

[Serializable]
public class RenameTagEto
{
    public required string originalTagName { get; set; }
    public required string replacementTagName { get; set; }
}
