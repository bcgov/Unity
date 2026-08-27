using System;
using System.Threading.Tasks;

namespace Unity.AI.Attachments;

public interface IAttachmentContentProvider
{
    Task<AttachmentContentStream> OpenAttachmentAsync(Guid submissionId, Guid fileId, string name);
}
