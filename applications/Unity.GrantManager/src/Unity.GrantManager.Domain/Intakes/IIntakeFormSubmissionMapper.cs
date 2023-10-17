using System.Threading.Tasks;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.Intakes
{
    public interface IIntakeFormSubmissionMapper
    {        
        string InitializeAvailableFormFields(ApplicationForm applicationForm, dynamic formVersion);

        IntakeMapping MapFormSubmissionFields(ApplicationForm applicationForm, dynamic formSubmission);
    }
}
