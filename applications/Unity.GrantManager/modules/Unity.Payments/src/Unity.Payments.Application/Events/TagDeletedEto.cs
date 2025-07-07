using System;

namespace Unity.Payments.Events;

[Serializable]
public class TagDeletedEto
{
    public required Guid TagId { get; set; }
}
