using Volo.Abp.Validation;

namespace Unity.GrantManager.Exceptions
{
    public class ApplicationFormSetupException : AbpValidationException
    {
        private const string FormSetupErrorMessage = "Application Form Setup Error";

        public ApplicationFormSetupException(string? message = null)
            : base([new(message ?? FormSetupErrorMessage)])
        {
            LogLevel = Microsoft.Extensions.Logging.LogLevel.Error;
        }
    }
}
