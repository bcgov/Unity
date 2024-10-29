using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.Intakes
{
    public interface IIntakeFormSubmissionManager
    {
        Task<Guid> ProcessFormSubmissionAsync(ApplicationForm applicationForm, dynamic formSubmission);
        Task ResyncSubmissionAttachments(Guid applicationId);
    }
}