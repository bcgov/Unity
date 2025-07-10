using System;

namespace Unity.Payments.Events;

[Serializable]
public class DeleteTagEto
{
    public required Guid TagId { get; set; }
}
