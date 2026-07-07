using System;
using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications;

public class QueueAttachmentSummaryRequestDto
{
    public Guid ApplicationId { get; set; }
    public List<Guid>? AttachmentIds { get; set; }
}
