using System;

namespace Unity.Payments.Events;

[Serializable]
public class DeleteTagEto
{
    public required string TagName { get; set; }
}
