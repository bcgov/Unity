using System;
using System.Runtime.Serialization;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Exceptions
{
    [Serializable]
    public class ApplicationFormSetupException : AbpValidationException
    {
        private const string FormSetupErrorMessage = "Application Form Setup Error";

        public ApplicationFormSetupException(string? message = null)
            : base([new(message ?? FormSetupErrorMessage)])
        {
            LogLevel = Microsoft.Extensions.Logging.LogLevel.Error;
        }

        protected ApplicationFormSetupException(SerializationInfo serializationEntries, StreamingContext context)
            : base([new(FormSetupErrorMessage)])
        {
            LogLevel = Microsoft.Extensions.Logging.LogLevel.Error;
        }
    }
}
