using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.Intakes
{
    public interface IIntakeFormSubmissionMapper
    {        
        Task<string> InitializeAvailableFormFields(ApplicationForm applicationForm, dynamic formVersion);

        Task<IntakeMapping> MapFormSubmissionFields(ApplicationForm applicationForm, dynamic formSubmission);
    }
}
