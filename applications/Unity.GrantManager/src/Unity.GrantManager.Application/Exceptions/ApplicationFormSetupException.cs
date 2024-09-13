using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Exceptions
{
    [Serializable]
    public class ApplicationFormSetupException : AbpValidationException
    {
        private const string FormSetupErrorMessage = "Application Form Setup Error";

        public ApplicationFormSetupException(string? message = null)
            : base(new List<ValidationResult> { new(message ?? FormSetupErrorMessage) })
        {
            LogLevel = Microsoft.Extensions.Logging.LogLevel.Error;
        }

        protected ApplicationFormSetupException(SerializationInfo serializationEntries, StreamingContext context) : base()
        {
            LogLevel = Microsoft.Extensions.Logging.LogLevel.Error;
        }
    }
}
