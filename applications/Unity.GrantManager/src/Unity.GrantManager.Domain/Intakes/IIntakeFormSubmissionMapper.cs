using System;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.Intakes
{
    public interface IIntakeFormSubmissionMapper
    {        
        string InitializeAvailableFormFields(dynamic formVersion);

        IntakeMapping MapFormSubmissionFields(ApplicationForm applicationForm, dynamic formSubmission, string? mapFormSubmissionFields);
        void SaveChefsFiles(dynamic formSubmission, Guid applicantId);
    }
}
