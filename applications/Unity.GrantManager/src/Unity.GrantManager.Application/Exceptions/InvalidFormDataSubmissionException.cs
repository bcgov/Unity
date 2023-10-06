using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Exceptions
{
    [Serializable]
    public class InvalidFormDataSubmissionException : AbpValidationException
    {
        private const string InvalidCommentMessage = "Invalid Form Submission Data";

        public InvalidFormDataSubmissionException(string? message = null)
            : base(new List<ValidationResult> { new ValidationResult(message ?? InvalidCommentMessage) })
        {
        }

        protected InvalidFormDataSubmissionException(SerializationInfo serializationEntries, StreamingContext context) : base(serializationEntries, context)
        {
        }

    }
}