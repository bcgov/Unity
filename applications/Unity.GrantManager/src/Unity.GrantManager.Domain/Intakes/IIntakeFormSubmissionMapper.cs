using System;
using System.Collections.Generic;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.Intakes
{
    public interface IIntakeFormSubmissionMapper
    {
        Dictionary<Guid, string> ExtractSubmissionFiles(dynamic formSubmission);
        string InitializeAvailableFormFields(dynamic formVersion);

        IntakeMapping MapFormSubmissionFields(ApplicationForm applicationForm, dynamic formSubmission, string? mapFormSubmissionFields);
        void SaveChefsFiles(dynamic formSubmission, Guid applicationId);
    }
}
