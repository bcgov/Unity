using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.Intakes;

public interface IChefsFileAttachmentStreamProvider
{
    Task<ChefsFileAttachmentStream> OpenAsync(Guid formSubmissionId, Guid chefsFileAttachmentId, string name);
}
