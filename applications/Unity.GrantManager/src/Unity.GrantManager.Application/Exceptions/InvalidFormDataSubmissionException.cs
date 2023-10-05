using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Exceptions
{
    public class InvalidFormDataSubmissionException : AbpValidationException
    {
        private const string InvalidCommentMessage = "Invalid Form Submission Data";

        public InvalidFormDataSubmissionException(string? message = null)
            : base(new List<ValidationResult> { new ValidationResult(message ?? InvalidCommentMessage) })
        {
        }
    }
}